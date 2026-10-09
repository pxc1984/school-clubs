<script lang="ts">
    import { toast } from "svelte-sonner";
    import CombineIcon from "@lucide/svelte/icons/combine";
    import EraserIcon from "@lucide/svelte/icons/eraser";
    import PencilIcon from "@lucide/svelte/icons/pencil";
    import Trash2Icon from "@lucide/svelte/icons/trash-2";
    import {
        deleteStudent,
        getStudent,
        listStudents,
        mergeStudents,
        updateStudent,
        type AdminStudentDetailsDto,
        type AdminStudentDto,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import EraseStudentDialog from "$lib/components/erase-student-dialog.svelte";
    import SearchPicker from "$lib/components/search-picker.svelte";
    import StudentDialog, {
        type StudentForm,
    } from "$lib/components/student-dialog.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import { Separator } from "$lib/components/ui/separator";
    import * as Sheet from "$lib/components/ui/sheet";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import { countOf, fullName } from "$lib/format";

    let {
        studentId,
        onClose,
        onChanged,
        onRemoved,
    }: {
        /** null — шторка закрыта */
        studentId: string | null;
        onClose: () => void;
        onChanged: () => Promise<void> | void;
        /** Ученика больше нет (удалён, объединён, стёрт) */
        onRemoved: (targetId?: string) => Promise<void> | void;
    } = $props();

    let student = $state<AdminStudentDetailsDto | null>(null);
    let loadError = $state<string | null>(null);

    $effect(() => {
        const id = studentId;
        student = null;
        loadError = null;
        if (!id) return;
        getStudent(id)
            .then((loaded) => {
                if (studentId === id) student = loaded;
            })
            .catch((e) => (loadError = errorMessage(e)));
    });

    let editOpen = $state(false);
    let deleteOpen = $state(false);
    let eraseOpen = $state(false);
    let mergeOpen = $state(false);
    let mergeTarget = $state<AdminStudentDto | null>(null);
    let mergeConfirmOpen = $state(false);

    async function save(form: StudentForm) {
        if (!student) return;
        student = await updateStudent(student.id, form);
        await onChanged();
    }

    function pickTarget(target: AdminStudentDto) {
        mergeTarget = target;
        mergeOpen = false;
        mergeConfirmOpen = true;
    }

    async function merge() {
        if (!student || !mergeTarget) return;
        const result = await mergeStudents(student.id, mergeTarget.id);
        toast.success("Ученики объединены", {
            description: result.droppedEntries
                ? `Перенесено отметок: ${result.movedEntries}; ${result.droppedEntries} отброшено — у ученика уже были отметки по тем же задачам.`
                : `Перенесено отметок: ${result.movedEntries}.`,
        });
        await onRemoved(result.targetId);
    }
</script>

<Sheet.Root
    open={studentId !== null}
    onOpenChange={(open) => {
        if (!open) onClose();
    }}
>
    <Sheet.Content class="w-full overflow-y-auto sm:max-w-md">
        {#if loadError}
            <Sheet.Header><Sheet.Title>Ученик</Sheet.Title></Sheet.Header>
            <Alert.Root variant="destructive" class="mx-4 w-auto">
                <Alert.Description>{loadError}</Alert.Description>
            </Alert.Root>
        {:else if !student}
            <Sheet.Header><Sheet.Title>Ученик</Sheet.Title></Sheet.Header>
            <div class="flex flex-col gap-3 px-4">
                <Skeleton class="h-6 w-2/3" />
                <Skeleton class="h-24 w-full" />
            </div>
        {:else}
            <Sheet.Header>
                <Sheet.Title>{fullName(student)}</Sheet.Title>
                <Sheet.Description>
                    {[
                        student.grade,
                        countOf(student.gradebookEntryCount, [
                            "отметка",
                            "отметки",
                            "отметок",
                        ]),
                    ]
                        .filter(Boolean)
                        .join(" · ")}
                </Sheet.Description>
            </Sheet.Header>

            <div class="flex flex-col gap-6 px-4 pb-6 text-sm">
                <section class="flex flex-col gap-2">
                    <h3 class="font-medium">Группы</h3>
                    {#if student.groups.length}
                        <ul class="flex flex-wrap gap-1.5">
                            {#each student.groups as group (group.id)}
                                <li>
                                    <Badge
                                        href="/admin/groups/{group.id}"
                                        variant="outline"
                                    >
                                        {group.title}
                                        {#if group.isArchived}(архив){/if}
                                    </Badge>
                                </li>
                            {/each}
                        </ul>
                    {:else}
                        <p class="text-muted-foreground">Ни в одной группе.</p>
                    {/if}
                </section>

                <section class="flex flex-col gap-2">
                    <h3 class="font-medium">Родители с доступом</h3>
                    {#if student.parents.length}
                        <ul class="flex flex-col gap-1">
                            {#each student.parents as parent (parent.id)}
                                <li>
                                    <a
                                        href="/admin/users?user={parent.id}"
                                        class="hover:underline"
                                    >
                                        {fullName(parent)}
                                    </a>
                                </li>
                            {/each}
                        </ul>
                    {:else}
                        <p class="text-muted-foreground">Нет.</p>
                    {/if}
                </section>

                <Separator />

                <section class="flex flex-wrap gap-2">
                    <Button
                        variant="outline"
                        size="sm"
                        onclick={() => (editOpen = true)}
                    >
                        <PencilIcon />
                        Изменить
                    </Button>
                    <Button
                        variant="outline"
                        size="sm"
                        onclick={() => (mergeOpen = true)}
                    >
                        <CombineIcon />
                        Это дубль…
                    </Button>
                    {#if student.gradebookEntryCount === 0}
                        <Button
                            variant="outline"
                            size="sm"
                            onclick={() => (deleteOpen = true)}
                        >
                            <Trash2Icon />
                            Удалить
                        </Button>
                    {/if}
                </section>

                <section
                    class="flex flex-col gap-2 rounded-lg border border-destructive/30 p-3"
                >
                    <h3 class="font-medium">Удаление по заявлению</h3>
                    <p class="text-muted-foreground">
                        Если родитель попросил удалить данные ребёнка (отзыв
                        согласия), сотрите всё, включая отметки и строки в
                        загруженных файлах.
                    </p>
                    <Button
                        variant="destructive"
                        size="sm"
                        class="w-fit"
                        onclick={() => (eraseOpen = true)}
                    >
                        <EraserIcon />
                        Удалить персональные данные
                    </Button>
                </section>
            </div>
        {/if}
    </Sheet.Content>
</Sheet.Root>

{#if student}
    {@const current = student}
    <StudentDialog
        bind:open={editOpen}
        title="Ученик"
        student={current}
        submitText="Сохранить"
        onSubmit={save}
    />

    <EraseStudentDialog
        bind:open={eraseOpen}
        student={current}
        onErased={() => onRemoved()}
    />

    <ConfirmDialog
        bind:open={deleteOpen}
        title="Удалить ученика?"
        description="Отметок у ученика нет — запись просто удалится, например заведённая по ошибке."
        confirmText="Удалить"
        destructive
        onConfirm={async () => {
            await deleteStudent(current.id);
            await onRemoved();
        }}
    />

    <Dialog.Root bind:open={mergeOpen}>
        <Dialog.Content class="sm:max-w-md">
            <Dialog.Header>
                <Dialog.Title>Объединить с другим учеником</Dialog.Title>
                <Dialog.Description>
                    Выберите основную запись. Отметки, группы и родители
                    перейдут к ней, а «{fullName(current)}» исчезнет.
                </Dialog.Description>
            </Dialog.Header>
            <SearchPicker
                search={(q) => listStudents(q)}
                key={(target) => target.id}
                exclude={[current.id]}
                onSelect={pickTarget}
                initialQuery={current.lastName}
                label="Поиск ученика"
            >
                {#snippet item(target)}
                    <span class="min-w-0 flex-1 truncate">
                        {fullName(target)}
                    </span>
                    <span class="truncate text-muted-foreground">
                        {[
                            target.grade,
                            ...target.groups.map((group) => group.title),
                        ]
                            .filter(Boolean)
                            .join(" · ")}
                    </span>
                {/snippet}
            </SearchPicker>
        </Dialog.Content>
    </Dialog.Root>

    <ConfirmDialog
        bind:open={mergeConfirmOpen}
        title="Объединить учеников?"
        description={mergeTarget
            ? `«${fullName(current)}» будет присоединён к «${fullName(mergeTarget)}». Если у обоих есть отметки по одной задаче, останется отметка основной записи.`
            : undefined}
        confirmText="Объединить"
        onConfirm={merge}
    />
{/if}
