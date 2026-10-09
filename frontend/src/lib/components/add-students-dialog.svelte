<script lang="ts">
    import { toast } from "svelte-sonner";
    import XIcon from "@lucide/svelte/icons/x";
    import {
        addAdminGroupStudents,
        listStudents,
        parseNames,
        parseNamesXlsx,
        type StudentListItemDto,
        type ParsedNameDto,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import type { NewStudentRequest } from "$lib/api/types";
    import SearchPicker from "$lib/components/search-picker.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import { Spinner } from "$lib/components/ui/spinner";
    import * as Tabs from "$lib/components/ui/tabs";
    import { Textarea } from "$lib/components/ui/textarea";
    import { countOf, fullName, shortName } from "$lib/format";

    let {
        open = $bindable(false),
        groupId,
        existingIds,
        onAdded,
    }: {
        open?: boolean;
        groupId: string;
        /** Уже в группе */
        existingIds: string[];
        onAdded: () => Promise<void> | void;
    } = $props();

    const NAMES_PLACEHOLDER = "Иванов Пётр Сергеевич 7Б\nПетрова Анна";

    let tab = $state("search");
    let picked = $state<StudentListItemDto[]>([]);
    let text = $state("");
    let parsed = $state<ParsedNameDto[] | null>(null);
    /** line → "new" | "skip" | studentId */
    let choices = $state<Record<number, string>>({});
    let busy = $state(false);

    $effect.pre(() => {
        if (!open) return;
        tab = "search";
        picked = [];
        text = "";
        parsed = null;
        choices = {};
    });

    function togglePick(student: StudentListItemDto) {
        picked = picked.some((p) => p.id === student.id)
            ? picked.filter((p) => p.id !== student.id)
            : [...picked, student];
    }

    let fileInput = $state<HTMLInputElement | null>(null);

    async function parse() {
        await preview(() => parseNames(text));
    }

    async function onFileSelected(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        const file = input.files?.[0];
        input.value = "";
        if (file) await preview(() => parseNamesXlsx(file));
    }

    async function preview(load: () => Promise<ParsedNameDto[]>) {
        busy = true;
        try {
            parsed = await load();
            // по умолчанию: совпадение в группе уже есть — пропустить,
            // есть дубль в базе — взять его, иначе завести нового
            choices = Object.fromEntries(
                parsed.map((line) => {
                    if (!line.student) return [line.line, "skip"];
                    const inGroup = line.matches.find((m) =>
                        existingIds.includes(m.id),
                    );
                    if (inGroup) return [line.line, "skip"];
                    if (line.matches.length === 1)
                        return [line.line, line.matches[0].id];
                    return [line.line, "new"];
                }),
            );
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }

    const listPlan = $derived.by(() => {
        const studentIds: string[] = [];
        const newStudents: NewStudentRequest[] = [];
        for (const line of parsed ?? []) {
            const choice = choices[line.line];
            if (choice === "skip" || !line.student) continue;
            if (choice === "new") newStudents.push(line.student);
            else if (!existingIds.includes(choice)) studentIds.push(choice);
        }
        return { studentIds: [...new Set(studentIds)], newStudents };
    });

    const total = $derived(
        tab === "search"
            ? picked.length
            : listPlan.studentIds.length + listPlan.newStudents.length,
    );

    async function submit() {
        busy = true;
        try {
            if (tab === "search")
                await addAdminGroupStudents(
                    groupId,
                    picked.map((student) => student.id),
                    [],
                );
            else
                await addAdminGroupStudents(
                    groupId,
                    listPlan.studentIds,
                    listPlan.newStudents,
                );
            toast.success(
                `Добавлено: ${countOf(total, ["ученик", "ученика", "учеников"])}`,
            );
            await onAdded();
            open = false;
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <Dialog.Header>
            <Dialog.Title>Добавить учеников</Dialog.Title>
        </Dialog.Header>

        <Tabs.Root bind:value={tab}>
            <Tabs.List class="mb-3">
                <Tabs.Trigger value="search">Найти в базе</Tabs.Trigger>
                <Tabs.Trigger value="list">Вставить список</Tabs.Trigger>
            </Tabs.List>

            <Tabs.Content value="search" class="flex flex-col gap-3">
                <SearchPicker
                    search={(q) => listStudents(q)}
                    key={(student) => student.id}
                    exclude={existingIds}
                    onSelect={togglePick}
                    label="Поиск ученика"
                >
                    {#snippet item(student)}
                        {@const isPicked = picked.some(
                            (p) => p.id === student.id,
                        )}
                        <span class="min-w-0 flex-1 truncate">
                            {fullName(student)}
                        </span>
                        {#if student.grade}
                            <span class="text-muted-foreground">
                                {student.grade}
                            </span>
                        {/if}
                        {#if isPicked}<Badge>выбран</Badge>{/if}
                    {/snippet}
                </SearchPicker>
                {#if picked.length}
                    <div class="flex flex-wrap gap-1.5">
                        {#each picked as student (student.id)}
                            <Badge variant="secondary" class="gap-1 pr-1">
                                {shortName(student)}
                                <button
                                    type="button"
                                    class="rounded-sm hover:bg-muted"
                                    aria-label="Убрать {shortName(student)}"
                                    onclick={() => togglePick(student)}
                                >
                                    <XIcon class="size-3" />
                                </button>
                            </Badge>
                        {/each}
                    </div>
                {/if}
            </Tabs.Content>

            <Tabs.Content value="list" class="flex flex-col gap-3">
                {#if !parsed}
                    <Field.Field>
                        <Field.Label for="names-text">
                            Список ФИО, по одному на строку
                        </Field.Label>
                        <Textarea
                            id="names-text"
                            rows={8}
                            placeholder={NAMES_PLACEHOLDER}
                            bind:value={text}
                        />
                        <Field.Description>
                            Можно скопировать столбец из Excel. Класс в конце
                            строки — по желанию.
                        </Field.Description>
                    </Field.Field>
                    <div class="flex flex-wrap gap-2">
                        <Button
                            variant="outline"
                            disabled={busy || !text.trim()}
                            onclick={parse}
                        >
                            {#if busy}<Spinner />{/if}
                            Разобрать
                        </Button>
                        <Button
                            variant="ghost"
                            disabled={busy}
                            onclick={() => fileInput?.click()}
                        >
                            Загрузить .xlsx
                        </Button>
                    </div>
                    <p class="text-xs text-muted-foreground">
                        В .xlsx берётся первый лист: строка — ученик, ФИО одной
                        ячейкой или по колонкам «Фамилия, Имя, Отчество, Класс».
                        Заголовок и колонка «№» пропускаются.
                    </p>
                    <input
                        bind:this={fileInput}
                        type="file"
                        accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        class="hidden"
                        onchange={onFileSelected}
                    />
                {:else}
                    <ul class="divide-y rounded-lg border text-sm">
                        {#each parsed as line (line.line)}
                            <li
                                class="flex flex-wrap items-center gap-3 px-3 py-2"
                            >
                                <div class="min-w-48 flex-1">
                                    {#if line.student}
                                        <div class="font-medium">
                                            {fullName(line.student)}
                                            {#if line.student.grade}
                                                <span
                                                    class="font-normal text-muted-foreground"
                                                >
                                                    {line.student.grade}
                                                </span>
                                            {/if}
                                        </div>
                                    {:else}
                                        <div class="text-destructive">
                                            «{line.source}» — {line.error}
                                        </div>
                                    {/if}
                                </div>
                                {#if line.student}
                                    <NativeSelect.Root
                                        class="w-64"
                                        aria-label="Что сделать со строкой {line.line}"
                                        bind:value={choices[line.line]}
                                    >
                                        <NativeSelect.Option value="new">
                                            Новый ученик
                                        </NativeSelect.Option>
                                        {#each line.matches as match (match.id)}
                                            <NativeSelect.Option
                                                value={match.id}
                                            >
                                                {existingIds.includes(match.id)
                                                    ? "Уже в группе: "
                                                    : "Есть в базе: "}{fullName(
                                                    match,
                                                )}{match.grade
                                                    ? `, ${match.grade}`
                                                    : ""}
                                            </NativeSelect.Option>
                                        {/each}
                                        <NativeSelect.Option value="skip">
                                            Пропустить
                                        </NativeSelect.Option>
                                    </NativeSelect.Root>
                                {/if}
                            </li>
                        {/each}
                    </ul>
                    <Button
                        variant="ghost"
                        size="sm"
                        class="w-fit"
                        onclick={() => (parsed = null)}
                    >
                        Изменить список
                    </Button>
                {/if}
            </Tabs.Content>
        </Tabs.Root>

        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button disabled={busy || !total} onclick={submit}>
                {#if busy}<Spinner />{/if}
                Добавить{total ? ` ${total}` : ""}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
