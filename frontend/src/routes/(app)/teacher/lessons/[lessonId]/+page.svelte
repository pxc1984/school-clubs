<script lang="ts">
    import { toast } from "svelte-sonner";
    import { goto, invalidateAll } from "$app/navigation";
    import CheckIcon from "@lucide/svelte/icons/check";
    import CircleAlertIcon from "@lucide/svelte/icons/circle-alert";
    import DownloadIcon from "@lucide/svelte/icons/download";
    import EllipsisIcon from "@lucide/svelte/icons/ellipsis";
    import EyeIcon from "@lucide/svelte/icons/eye";
    import EyeOffIcon from "@lucide/svelte/icons/eye-off";
    import FileSpreadsheetIcon from "@lucide/svelte/icons/file-spreadsheet";
    import PencilIcon from "@lucide/svelte/icons/pencil";
    import Trash2Icon from "@lucide/svelte/icons/trash-2";
    import UploadIcon from "@lucide/svelte/icons/upload";
    import { errorMessage } from "$lib/api/client";
    import {
        addTask,
        deleteLesson,
        deleteTask,
        importFileUrl,
        lessonTemplateUrl,
        publishGradebook,
        unpublishGradebook,
    } from "$lib/api/teacher";
    import type { TaskDto } from "$lib/api/types";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import GradeGrid, {
        type SaveState,
    } from "$lib/components/grade-grid.svelte";
    import ImportDialog from "$lib/components/import-dialog.svelte";
    import LessonDialog from "$lib/components/lesson-dialog.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import TaskDialog from "$lib/components/task-dialog.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as DropdownMenu from "$lib/components/ui/dropdown-menu";
    import * as Empty from "$lib/components/ui/empty";
    import { Spinner } from "$lib/components/ui/spinner";
    import {
        countOf,
        formatDate,
        formatDateTime,
        formatTime,
        formatWeekday,
    } from "$lib/format";

    let { data } = $props();

    const lesson = $derived(data.lesson);
    const gradebook = $derived(data.gradebook);
    const published = $derived(gradebook.publishedAt !== null);

    let grid = $state<GradeGrid | null>(null);
    let saveState = $state<SaveState>("saved");

    const title = $derived(
        `${formatDate(lesson.date)}, ${formatWeekday(lesson.date)}${lesson.time ? ` · ${formatTime(lesson.time)}` : ""}`,
    );

    /** Перед действиями с сервером дожимаем несохранённые отметки */
    async function flushOrToast(): Promise<boolean> {
        try {
            await grid?.flush();
            return true;
        } catch (e) {
            toast.error(`Отметки не сохранены: ${errorMessage(e)}`);
            return false;
        }
    }

    // ---------- публикация ----------

    let publishBusy = $state(false);
    let incomplete = $state(0);
    let publishConfirmOpen = $state(false);
    let unpublishOpen = $state(false);

    async function requestPublish() {
        if (!(await flushOrToast())) return;
        incomplete = grid?.incompleteRows() ?? 0;
        if (incomplete) publishConfirmOpen = true;
        else await doPublish();
    }

    async function doPublish() {
        publishBusy = true;
        try {
            await publishGradebook(lesson.id);
            await invalidateAll();
            toast.success("Журнал опубликован", {
                description: "Родители видят отметки этого занятия.",
            });
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            publishBusy = false;
        }
    }

    async function doUnpublish() {
        await unpublishGradebook(lesson.id);
        await invalidateAll();
    }

    // ---------- задачи ----------

    let editingTask = $state<TaskDto | null>(null);
    let taskOpen = $state(false);
    let deleting = $state<{ task: TaskDto; marks: number } | null>(null);
    let deleteTaskOpen = $state(false);

    async function onAddTask() {
        try {
            await addTask(lesson.id, null);
            await invalidateAll();
        } catch (e) {
            toast.error(errorMessage(e));
        }
    }

    async function onDeleteTask() {
        if (!deleting) return;
        if (!(await flushOrToast())) return;
        await deleteTask(lesson.id, deleting.task.id);
        await invalidateAll();
    }

    // ---------- занятие ----------

    let editOpen = $state(false);
    let importOpen = $state(false);
    let deleteLessonOpen = $state(false);

    async function onDeleteLesson() {
        await deleteLesson(lesson.id);
        await goto(`/teacher/groups/${lesson.groupId}`);
        toast.success("Занятие удалено");
    }
</script>

<svelte:head>
    <title>{lesson.groupTitle}, {formatDate(lesson.date)} — Кружки 218</title>
</svelte:head>

<PageHeader
    {title}
    description={lesson.topic}
    back={{
        href: `/teacher/groups/${lesson.groupId}`,
        label: lesson.groupTitle,
    }}
>
    {#snippet badges()}
        {#if published}
            <Badge>Опубликован</Badge>
        {:else}
            <Badge variant="secondary">Черновик</Badge>
        {/if}
        <span
            class="inline-flex items-center gap-1 text-xs text-muted-foreground"
            aria-live="polite"
        >
            {#if saveState === "saving"}
                <Spinner class="size-3" /> Сохраняю…
            {:else if saveState === "dirty"}
                Есть несохранённые изменения
            {:else if saveState === "error"}
                <span class="inline-flex items-center gap-1 text-destructive">
                    <CircleAlertIcon class="size-3" /> Ошибка сохранения
                </span>
            {:else}
                <CheckIcon class="size-3" /> Сохранено
            {/if}
        </span>
    {/snippet}
    {#snippet actions()}
        {#if published}
            <Button variant="outline" onclick={() => (unpublishOpen = true)}>
                <EyeOffIcon />
                Снять с публикации
            </Button>
        {:else}
            <Button
                disabled={publishBusy || !gradebook.students.length}
                onclick={requestPublish}
            >
                {#if publishBusy}<Spinner />{:else}<EyeIcon />{/if}
                Опубликовать
            </Button>
        {/if}
        <Button variant="outline" onclick={() => (importOpen = true)}>
            <UploadIcon />
            Загрузить XLSX
        </Button>
        <DropdownMenu.Root>
            <DropdownMenu.Trigger>
                {#snippet child({ props })}
                    <Button {...props} variant="outline" size="icon">
                        <EllipsisIcon />
                        <span class="sr-only">Ещё</span>
                    </Button>
                {/snippet}
            </DropdownMenu.Trigger>
            <DropdownMenu.Content align="end">
                <DropdownMenu.Item>
                    {#snippet child({ props })}
                        <a
                            {...props}
                            href={lessonTemplateUrl(lesson.id)}
                            download
                            data-sveltekit-reload
                        >
                            <DownloadIcon />
                            Скачать трафарет
                        </a>
                    {/snippet}
                </DropdownMenu.Item>
                <DropdownMenu.Item onSelect={() => (editOpen = true)}>
                    <PencilIcon />
                    Дата, время и тема
                </DropdownMenu.Item>
                <DropdownMenu.Separator />
                <DropdownMenu.Item
                    variant="destructive"
                    disabled={gradebook.entries.length > 0 ||
                        saveState !== "saved"}
                    onSelect={() => (deleteLessonOpen = true)}
                >
                    <Trash2Icon />
                    Удалить занятие
                </DropdownMenu.Item>
            </DropdownMenu.Content>
        </DropdownMenu.Root>
    {/snippet}
</PageHeader>

{#if published}
    <p class="-mt-3 mb-4 text-sm text-muted-foreground">
        Опубликован {formatDateTime(gradebook.publishedAt!)}. Исправления сразу
        видны родителям.
    </p>
{:else if gradebook.students.length}
    <p class="-mt-3 mb-4 text-sm text-muted-foreground">
        Черновик: родители пока не видят отметки этого занятия. Заполните журнал
        и нажмите «Опубликовать».
    </p>
{/if}

{#if !gradebook.students.length}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Title>В группе нет учеников</Empty.Title>
            <Empty.Description>
                Добавьте учеников на странице группы — и журнал заполнится
                строками.
            </Empty.Description>
        </Empty.Header>
        <Empty.Content>
            <Button
                href="/teacher/groups/{lesson.groupId}?tab=students"
                variant="outline"
            >
                К ученикам группы
            </Button>
        </Empty.Content>
    </Empty.Root>
{:else}
    <GradeGrid
        bind:this={grid}
        bind:saveState
        lessonId={lesson.id}
        students={gradebook.students}
        tasks={gradebook.tasks}
        entries={gradebook.entries}
        attendance={gradebook.attendance}
        {onAddTask}
        onEditTask={(task) => {
            editingTask = task;
            taskOpen = true;
        }}
        onDeleteTask={(task, marks) => {
            deleting = { task, marks };
            deleteTaskOpen = true;
        }}
    />
{/if}

{#if data.imports.length}
    <section class="mt-8">
        <h2 class="mb-2 text-sm font-medium">Загруженные файлы</h2>
        <ul class="divide-y rounded-lg border text-sm">
            {#each data.imports as file (file.uploadId)}
                <li class="flex flex-wrap items-center gap-2 px-4 py-2">
                    <FileSpreadsheetIcon class="size-4 text-muted-foreground" />
                    <a
                        href={importFileUrl(lesson.id, file.uploadId)}
                        download
                        data-sveltekit-reload
                        class="min-w-0 truncate hover:underline"
                    >
                        {file.fileName}
                    </a>
                    <span class="text-muted-foreground">
                        {formatDateTime(file.uploadedAt)}
                    </span>
                    {#if file.attached}
                        <Badge variant="secondary">в журнале</Badge>
                    {/if}
                </li>
            {/each}
        </ul>
    </section>
{/if}

<ImportDialog
    bind:open={importOpen}
    lessonId={lesson.id}
    students={gradebook.students}
    onApplied={async () => {
        await invalidateAll();
    }}
/>

<LessonDialog
    bind:open={editOpen}
    mode={{ kind: "edit", lesson }}
    onSaved={async () => {
        await invalidateAll();
    }}
/>

<TaskDialog
    bind:open={taskOpen}
    lessonId={lesson.id}
    task={editingTask}
    onSaved={async () => {
        await invalidateAll();
    }}
/>

<ConfirmDialog
    bind:open={deleteTaskOpen}
    title="Удалить задачу {deleting?.task.number ?? ''}?"
    description={deleting?.marks
        ? `Вместе с ней удалятся ${countOf(deleting.marks, ["отметка", "отметки", "отметок"])}.`
        : "Отметок по ней нет."}
    confirmText="Удалить"
    destructive
    onConfirm={onDeleteTask}
/>

<ConfirmDialog
    bind:open={publishConfirmOpen}
    title="Не все строки заполнены"
    description="У {countOf(incomplete, [
        'ученика',
        'учеников',
        'учеников',
    ])} отмечены не все задачи. Пустые клетки родители увидят как «нет отметки». Опубликовать всё равно?"
    confirmText="Опубликовать"
    onConfirm={doPublish}
/>

<ConfirmDialog
    bind:open={unpublishOpen}
    title="Снять журнал с публикации?"
    description="Родители перестанут видеть отметки этого занятия, пока вы не опубликуете его снова."
    confirmText="Снять"
    onConfirm={doUnpublish}
/>

<ConfirmDialog
    bind:open={deleteLessonOpen}
    title="Удалить занятие?"
    description="Занятие {formatDate(
        lesson.date,
    )} будет удалено вместе с пустым журналом."
    confirmText="Удалить"
    destructive
    onConfirm={onDeleteLesson}
/>
