<script lang="ts">
    import { toast } from "svelte-sonner";
    import ArrowRightIcon from "@lucide/svelte/icons/arrow-right";
    import FileSpreadsheetIcon from "@lucide/svelte/icons/file-spreadsheet";
    import { errorMessage } from "$lib/api/client";
    import {
        applyImport,
        previewImport,
        type ImportPreviewDto,
        type ImportResultDto,
    } from "$lib/api/teacher";
    import type { Guid, StudentDto } from "$lib/api/types";
    import GradeChip from "$lib/components/grade-chip.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import { Spinner } from "$lib/components/ui/spinner";
    import * as Table from "$lib/components/ui/table";
    import { countOf, fullName, shortName } from "$lib/format";

    const MAX_SIZE = 2 * 1024 * 1024;

    let {
        open = $bindable(false),
        lessonId,
        students,
        onApplied,
    }: {
        open?: boolean;
        lessonId: Guid;
        /** Ученики журнала — по ним подписываем изменения (в ответе только id) */
        students: StudentDto[];
        onApplied: (result: ImportResultDto) => Promise<void> | void;
    } = $props();

    const studentNames = $derived(
        new Map(students.map((student) => [student.id, shortName(student)])),
    );

    let file = $state<File | null>(null);
    let fileError = $state<string | null>(null);
    let preview = $state<ImportPreviewDto | null>(null);
    let busy = $state(false);

    let confirmOtherLesson = $state(false);
    let createTasks = $state(true);
    let publish = $state(false);
    /** row → studentId; "" — пропустить строку */
    let mappings = $state<Record<number, string>>({});

    $effect.pre(() => {
        if (!open) return;
        file = null;
        fileError = null;
        preview = null;
        confirmOtherLesson = false;
        createTasks = true;
        publish = false;
        mappings = {};
    });

    function onFileChange(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        const picked = input.files?.[0] ?? null;
        fileError = null;
        file = null;
        if (!picked) return;
        if (!picked.name.toLowerCase().endsWith(".xlsx"))
            fileError = "Нужен файл Excel в формате .xlsx";
        else if (picked.size > MAX_SIZE)
            fileError = "Файл больше 2 МБ — это точно журнал?";
        else file = picked;
    }

    async function check(event: SubmitEvent) {
        event.preventDefault();
        if (!file) return;
        busy = true;
        try {
            preview = await previewImport(lessonId, file);
            mappings = Object.fromEntries(
                preview.unrecognizedRows.map((row) => [row.row, ""]),
            );
        } catch (e) {
            fileError = errorMessage(e);
        } finally {
            busy = false;
        }
    }

    const visibleChanges = $derived(
        preview?.changes.filter((change) => change.kind !== "Unchanged") ?? [],
    );
    const counts = $derived({
        new: visibleChanges.filter((change) => change.kind === "New").length,
        changed: visibleChanges.filter((change) => change.kind === "Changed")
            .length,
        unchanged: (preview?.changes.length ?? 0) - visibleChanges.length,
    });
    const mappedRows = $derived(
        Object.values(mappings).filter((studentId) => studentId).length,
    );
    const canApply = $derived(
        !!preview &&
            !preview.errors.length &&
            (!preview.lessonMismatch || confirmOtherLesson) &&
            (visibleChanges.length > 0 || mappedRows > 0 || publish),
    );

    async function apply() {
        if (!preview) return;
        busy = true;
        try {
            const result = await applyImport(lessonId, preview.uploadId, {
                rowMappings: Object.entries(mappings).map(
                    ([row, studentId]) => ({
                        row: Number(row),
                        studentId: studentId || null,
                    }),
                ),
                confirmLessonMismatch: confirmOtherLesson,
                createMissingTasks: createTasks,
                publish,
            });
            toast.success("Журнал обновлён", {
                description: `Новых отметок: ${result.created}, изменено: ${result.updated}`,
            });
            await onApplied(result);
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
            <Dialog.Title>Загрузка журнала из Excel</Dialog.Title>
            <Dialog.Description>
                {#if preview}
                    Проверьте, что изменится, прежде чем применять.
                {:else}
                    Заполненный трафарет этого занятия. Ничего не изменится до
                    подтверждения.
                {/if}
            </Dialog.Description>
        </Dialog.Header>

        {#if !preview}
            <form id="import-pick" onsubmit={check}>
                <Field.Field data-invalid={!!fileError}>
                    <Field.Label for="import-file">Файл .xlsx</Field.Label>
                    <Input
                        id="import-file"
                        type="file"
                        accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        onchange={onFileChange}
                        aria-invalid={!!fileError}
                    />
                    <Field.Description>До 2 МБ</Field.Description>
                    <Field.Error>{fileError}</Field.Error>
                </Field.Field>
            </form>
        {:else}
            <div class="flex flex-col gap-5 text-sm">
                <div class="flex flex-wrap items-center gap-2">
                    <FileSpreadsheetIcon class="size-4 text-muted-foreground" />
                    <span class="font-medium">{preview.fileName}</span>
                </div>

                {#each preview.errors as error (error)}
                    <Alert.Root variant="destructive">
                        <Alert.Title>Применить нельзя</Alert.Title>
                        <Alert.Description>{error}</Alert.Description>
                    </Alert.Root>
                {/each}

                {#if preview.lessonMismatch}
                    <Alert.Root variant="destructive">
                        <Alert.Title>Файл от другого занятия</Alert.Title>
                        <Alert.Description>
                            {preview.lessonMismatch}. Возможно, выбран не тот
                            файл.
                        </Alert.Description>
                    </Alert.Root>
                    <Field.Field orientation="horizontal">
                        <Checkbox
                            id="import-confirm-other"
                            bind:checked={confirmOtherLesson}
                        />
                        <Field.Label
                            for="import-confirm-other"
                            class="font-normal"
                        >
                            Всё равно загрузить в это занятие
                        </Field.Label>
                    </Field.Field>
                {/if}

                {#if preview.foreignFile}
                    <Alert.Root>
                        <Alert.Title>Файл не из трафарета</Alert.Title>
                        <Alert.Description>
                            Ученики сопоставлены по ФИО, задачи — по номерам
                            столбцов. Проверьте результат внимательнее.
                        </Alert.Description>
                    </Alert.Root>
                {/if}

                {#each preview.warnings as warning (warning)}
                    <Alert.Root>
                        <Alert.Description>{warning}</Alert.Description>
                    </Alert.Root>
                {/each}

                {#if preview.missingTaskNumbers.length}
                    <Field.Field orientation="horizontal">
                        <Checkbox
                            id="import-create-tasks"
                            bind:checked={createTasks}
                        />
                        <Field.Content>
                            <Field.Label
                                for="import-create-tasks"
                                class="font-normal"
                            >
                                Добавить в занятие задачи {preview.missingTaskNumbers.join(
                                    ", ",
                                )}
                            </Field.Label>
                            <Field.Description>
                                Они есть в файле, но не в журнале. Без них
                                отметки по этим задачам не загрузятся.
                            </Field.Description>
                        </Field.Content>
                    </Field.Field>
                {/if}

                <section class="flex flex-col gap-2">
                    <h3 class="font-medium">Отметки</h3>
                    <p class="text-muted-foreground">
                        Новых — {counts.new}, изменится — {counts.changed}, без
                        изменений — {counts.unchanged}.
                    </p>
                    {#if visibleChanges.length}
                        <div class="max-h-64 overflow-y-auto rounded-lg border">
                            <Table.Root>
                                <Table.Header>
                                    <Table.Row>
                                        <Table.Head>Ученик</Table.Head>
                                        <Table.Head>Задача</Table.Head>
                                        <Table.Head>Было → стало</Table.Head>
                                    </Table.Row>
                                </Table.Header>
                                <Table.Body>
                                    {#each visibleChanges as change (`${change.studentId}:${change.taskNumber}`)}
                                        <Table.Row
                                            class={change.taskId === null &&
                                            !createTasks
                                                ? "opacity-50"
                                                : undefined}
                                        >
                                            <Table.Cell>
                                                {studentNames.get(
                                                    change.studentId,
                                                ) ?? `строка ${change.row}`}
                                            </Table.Cell>
                                            <Table.Cell>
                                                {change.taskNumber}
                                                {#if change.taskId === null}
                                                    <Badge variant="outline">
                                                        новая
                                                    </Badge>
                                                {/if}
                                            </Table.Cell>
                                            <Table.Cell>
                                                <span
                                                    class="inline-flex items-center gap-1.5"
                                                >
                                                    <GradeChip
                                                        status={change.previousStatus}
                                                    />
                                                    <ArrowRightIcon
                                                        class="size-3.5 text-muted-foreground"
                                                    />
                                                    <GradeChip
                                                        status={change.status}
                                                    />
                                                </span>
                                            </Table.Cell>
                                        </Table.Row>
                                    {/each}
                                </Table.Body>
                            </Table.Root>
                        </div>
                    {/if}
                </section>

                {#if preview.unrecognizedRows.length}
                    <section class="flex flex-col gap-3">
                        <div>
                            <h3 class="font-medium">Не узнали учеников</h3>
                            <p class="text-muted-foreground">
                                Выберите, чья это строка, или пропустите её.
                            </p>
                        </div>
                        {#each preview.unrecognizedRows as row (row.row)}
                            <div class="flex flex-wrap items-center gap-3">
                                <span class="min-w-40 flex-1">
                                    <span class="text-muted-foreground">
                                        строка {row.row}:
                                    </span>
                                    «{row.name}»
                                    <span
                                        class="block text-xs text-muted-foreground"
                                    >
                                        {row.reason}
                                    </span>
                                </span>
                                <NativeSelect.Root
                                    class="w-64"
                                    aria-label="Ученик для строки {row.row}"
                                    bind:value={mappings[row.row]}
                                >
                                    <NativeSelect.Option value="">
                                        Пропустить строку
                                    </NativeSelect.Option>
                                    {#each row.candidates as candidate (candidate.id)}
                                        <NativeSelect.Option
                                            value={candidate.id}
                                        >
                                            {fullName(
                                                candidate,
                                            )}{candidate.grade
                                                ? `, ${candidate.grade}`
                                                : ""}
                                        </NativeSelect.Option>
                                    {/each}
                                </NativeSelect.Root>
                            </div>
                        {/each}
                    </section>
                {/if}

                {#if preview.missingStudents.length}
                    <section class="flex flex-col gap-1">
                        <h3 class="font-medium">Нет в файле</h3>
                        <p class="text-muted-foreground">
                            Отметки этих учеников не изменятся:
                            {preview.missingStudents
                                .map((student) => shortName(student))
                                .join(", ")}.
                        </p>
                    </section>
                {/if}

                {#if preview.cellErrors.length}
                    <section class="flex flex-col gap-2">
                        <h3 class="font-medium">
                            Не прочитали {countOf(preview.cellErrors.length, [
                                "ячейку",
                                "ячейки",
                                "ячеек",
                            ])}
                        </h3>
                        <p class="text-muted-foreground">
                            Они будут пропущены. Допустимые значения: + +. -. -
                            или пусто.
                        </p>
                        <div class="max-h-48 overflow-y-auto rounded-lg border">
                            <Table.Root>
                                <Table.Body>
                                    {#each preview.cellErrors as cellError (cellError)}
                                        <Table.Row>
                                            <Table.Cell
                                                class="whitespace-normal"
                                            >
                                                {cellError}
                                            </Table.Cell>
                                        </Table.Row>
                                    {/each}
                                </Table.Body>
                            </Table.Root>
                        </div>
                    </section>
                {/if}

                <Field.Field orientation="horizontal">
                    <Checkbox id="import-publish" bind:checked={publish} />
                    <Field.Label for="import-publish" class="font-normal">
                        Сразу опубликовать — родители увидят отметки
                    </Field.Label>
                </Field.Field>
            </div>
        {/if}

        <Dialog.Footer>
            {#if preview}
                <Button
                    variant="outline"
                    disabled={busy}
                    onclick={() => {
                        preview = null;
                        file = null;
                    }}
                >
                    Другой файл
                </Button>
                <Button disabled={busy || !canApply} onclick={apply}>
                    {#if busy}<Spinner />{/if}
                    Применить
                </Button>
            {:else}
                <Button variant="outline" onclick={() => (open = false)}>
                    Отмена
                </Button>
                <Button
                    type="submit"
                    form="import-pick"
                    disabled={busy || !file}
                >
                    {#if busy}<Spinner />{/if}
                    Проверить файл
                </Button>
            {/if}
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
