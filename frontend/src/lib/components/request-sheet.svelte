<script lang="ts">
    import { toast } from "svelte-sonner";
    import CheckIcon from "@lucide/svelte/icons/check";
    import XIcon from "@lucide/svelte/icons/x";
    import {
        approveRequest,
        listStudents,
        rejectRequest,
        type AdminAccessRequestDto,
        type AdminStudentDto,
        type ApproveRequest,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import type { Relation, StudentDto } from "$lib/api/types";
    import RequestStatusBadge from "$lib/components/request-status-badge.svelte";
    import SearchPicker from "$lib/components/search-picker.svelte";
    import TextPromptDialog from "$lib/components/text-prompt-dialog.svelte";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import * as RadioGroup from "$lib/components/ui/radio-group";
    import { Separator } from "$lib/components/ui/separator";
    import * as Sheet from "$lib/components/ui/sheet";
    import { Spinner } from "$lib/components/ui/spinner";
    import { formatDateTime, fullName } from "$lib/format";

    const RELATION_LABELS: Record<Relation, string> = {
        Mother: "мама",
        Father: "папа",
        Guardian: "опекун",
    };

    let {
        request,
        onClose,
        onDecided,
    }: {
        /** null — шторка закрыта */
        request: AdminAccessRequestDto | null;
        onClose: () => void;
        onDecided: () => Promise<void> | void;
    } = $props();

    /** id подсказки, «search» — найденный вручную, «new» — завести нового */
    let choice = $state("");
    let found = $state<AdminStudentDto | null>(null);
    let busy = $state(false);
    let rejectOpen = $state(false);

    $effect.pre(() => {
        const id = request?.id;
        if (!id) return;
        choice = request?.suggestions[0]?.id ?? "search";
        found = null;
    });

    const childName = $derived(
        request
            ? fullName({
                  lastName: request.childLastName,
                  firstName: request.childFirstName,
                  middleName: request.childMiddleName,
              })
            : "",
    );

    const approval = $derived.by((): ApproveRequest | null => {
        if (!request) return null;
        if (choice === "new")
            return {
                studentId: null,
                newStudent: {
                    lastName: request.childLastName,
                    firstName: request.childFirstName,
                    middleName: request.childMiddleName,
                    grade: request.grade,
                },
            };
        if (choice === "search")
            return found ? { studentId: found.id, newStudent: null } : null;
        return { studentId: choice, newStudent: null };
    });

    async function approve() {
        if (!request || !approval) return;
        busy = true;
        try {
            await approveRequest(request.id, approval);
            toast.success("Доступ открыт", {
                description: `${fullName(request.parent)} видит отметки: ${childName}.`,
            });
            await onDecided();
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }

    async function reject(reason: string) {
        if (!request) return;
        await rejectRequest(request.id, reason);
        toast.success("Запрос отклонён");
        await onDecided();
    }

    function studentLabel(student: StudentDto): string {
        return `${fullName(student)}${student.grade ? `, ${student.grade}` : ""}`;
    }
</script>

<Sheet.Root
    open={request !== null}
    onOpenChange={(open) => {
        if (!open) onClose();
    }}
>
    <Sheet.Content class="w-full overflow-y-auto sm:max-w-lg">
        {#if request}
            <Sheet.Header>
                <Sheet.Title class="flex flex-wrap items-center gap-2">
                    {childName}
                    <RequestStatusBadge status={request.status} />
                </Sheet.Title>
                <Sheet.Description>
                    {request.groupTitle}{request.grade
                        ? ` · ${request.grade}`
                        : ""}
                </Sheet.Description>
            </Sheet.Header>

            <div class="flex flex-col gap-6 px-4 pb-6 text-sm">
                <dl class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
                    <dt class="text-muted-foreground">Родитель</dt>
                    <dd>
                        <a
                            href="/admin/users?user={request.parent.id}"
                            class="hover:underline"
                        >
                            {fullName(request.parent)}
                        </a>
                        ({RELATION_LABELS[request.relation]})
                    </dd>
                    <dt class="text-muted-foreground">Контакты</dt>
                    <dd>
                        {request.parent.email}{request.parent.phone
                            ? `, ${request.parent.phone}`
                            : ""}
                    </dd>
                    {#if request.parent.children.length}
                        <dt class="text-muted-foreground">Уже доступ к</dt>
                        <dd>
                            {request.parent.children
                                .map((child) => studentLabel(child))
                                .join("; ")}
                        </dd>
                    {/if}
                    <dt class="text-muted-foreground">Отправлен</dt>
                    <dd>{formatDateTime(request.createdAt)}</dd>
                    {#if request.decidedAt}
                        <dt class="text-muted-foreground">Решение</dt>
                        <dd>{formatDateTime(request.decidedAt)}</dd>
                    {/if}
                </dl>

                {#if request.comment}
                    <blockquote
                        class="border-l-2 pl-3 whitespace-pre-line text-muted-foreground"
                    >
                        {request.comment}
                    </blockquote>
                {/if}

                {#if request.rejectReason}
                    <p>
                        <span class="text-muted-foreground"
                            >Причина отказа:</span
                        >
                        {request.rejectReason}
                    </p>
                {/if}

                {#if request.status === "Pending"}
                    <Separator />
                    <section class="flex flex-col gap-3">
                        <h3 class="font-medium">Кто этот ребёнок?</h3>
                        <RadioGroup.Root bind:value={choice}>
                            {#each request.suggestions as suggestion (suggestion.id)}
                                <Field.Field orientation="horizontal">
                                    <RadioGroup.Item
                                        id="choice-{suggestion.id}"
                                        value={suggestion.id}
                                    />
                                    <Field.Label
                                        for="choice-{suggestion.id}"
                                        class="font-normal"
                                    >
                                        {studentLabel(suggestion)}
                                        <span class="text-muted-foreground">
                                            — в группе «{request.groupTitle}»
                                        </span>
                                    </Field.Label>
                                </Field.Field>
                            {/each}
                            <Field.Field orientation="horizontal">
                                <RadioGroup.Item
                                    id="choice-search"
                                    value="search"
                                />
                                <Field.Label
                                    for="choice-search"
                                    class="font-normal"
                                >
                                    {found
                                        ? `Найден: ${studentLabel(found)}`
                                        : "Найти в базе учеников"}
                                </Field.Label>
                            </Field.Field>
                            <Field.Field orientation="horizontal">
                                <RadioGroup.Item id="choice-new" value="new" />
                                <Field.Label
                                    for="choice-new"
                                    class="font-normal"
                                >
                                    Новый ученик «{childName}» в группе «{request.groupTitle}»
                                </Field.Label>
                            </Field.Field>
                        </RadioGroup.Root>

                        {#if choice === "search"}
                            <SearchPicker
                                search={(q) => listStudents(q)}
                                key={(student) => student.id}
                                selected={found?.id ?? null}
                                onSelect={(student) => (found = student)}
                                initialQuery={request.childLastName}
                                label="Поиск ученика"
                            >
                                {#snippet item(student)}
                                    <span class="min-w-0 flex-1 truncate">
                                        {fullName(student)}
                                    </span>
                                    <span
                                        class="truncate text-muted-foreground"
                                    >
                                        {[
                                            student.grade,
                                            ...student.groups.map(
                                                (group) => group.title,
                                            ),
                                        ]
                                            .filter(Boolean)
                                            .join(" · ")}
                                    </span>
                                {/snippet}
                            </SearchPicker>
                        {/if}
                    </section>

                    <div class="flex flex-wrap gap-2">
                        <Button disabled={busy || !approval} onclick={approve}>
                            {#if busy}<Spinner />{:else}<CheckIcon />{/if}
                            Одобрить
                        </Button>
                        <Button
                            variant="outline"
                            disabled={busy}
                            onclick={() => (rejectOpen = true)}
                        >
                            <XIcon />
                            Отклонить
                        </Button>
                    </div>
                {/if}
            </div>
        {/if}
    </Sheet.Content>
</Sheet.Root>

<TextPromptDialog
    bind:open={rejectOpen}
    title="Отклонить запрос"
    description="Родитель увидит причину в списке своих запросов."
    label="Причина"
    placeholder="Например: в группе нет ученика с такой фамилией"
    required
    maxlength={500}
    submitText="Отклонить"
    onSubmit={reject}
/>
