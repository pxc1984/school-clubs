<script lang="ts">
    import { toast } from "svelte-sonner";
    import { goto, invalidateAll } from "$app/navigation";
    import DownloadIcon from "@lucide/svelte/icons/download";
    import PlusIcon from "@lucide/svelte/icons/plus";
    import Trash2Icon from "@lucide/svelte/icons/trash-2";
    import UserPlusIcon from "@lucide/svelte/icons/user-plus";
    import {
        addGroupStudent,
        groupTemplateUrl,
        removeGroupStudent,
        type GroupMatrixDto,
    } from "$lib/api/teacher";
    import type { StudentDto } from "$lib/api/types";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import LessonDialog from "$lib/components/lesson-dialog.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import StudentDialog, {
        type StudentForm,
    } from "$lib/components/student-dialog.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button, buttonVariants } from "$lib/components/ui/button";
    import * as Empty from "$lib/components/ui/empty";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import * as Popover from "$lib/components/ui/popover";
    import { Progress } from "$lib/components/ui/progress";
    import * as Table from "$lib/components/ui/table";
    import * as Tabs from "$lib/components/ui/tabs";
    import {
        formatDate,
        formatShortDate,
        formatTime,
        formatWeekday,
        fullName,
        shortName,
        today,
    } from "$lib/format";
    import { cn } from "$lib/utils";

    let { data } = $props();

    const groupId = $derived(data.group.id);
    // число задач по умолчанию — как на последнем занятии
    const lastTaskCount = $derived(data.lessons[0]?.taskCount ?? 5);

    function selectTab(tab: string) {
        void goto(`?tab=${tab}`, {
            replaceState: true,
            noScroll: true,
            keepFocus: true,
        });
    }

    let lessonOpen = $state(false);
    let templateTasks = $derived(lastTaskCount);

    let studentOpen = $state(false);
    let removing = $state<StudentDto | null>(null);
    let removeOpen = $state(false);

    async function addStudent(form: StudentForm) {
        const student = await addGroupStudent(groupId, form);
        await invalidateAll();
        toast.success(`${shortName(student)} добавлен(а) в группу`);
    }

    async function removeStudent() {
        if (!removing) return;
        await removeGroupStudent(groupId, removing.id);
        await invalidateAll();
    }

    type MatrixCell = GroupMatrixDto["cells"][number];

    const matrixCells = $derived(
        new Map(
            (data.summary?.cells ?? []).map((cell) => [
                `${cell.studentId}:${cell.lessonId}`,
                cell,
            ]),
        ),
    );

    function matrixCell(studentId: string, lessonId: string) {
        return matrixCells.get(`${studentId}:${lessonId}`);
    }

    /** «Решил X из Y» по занятиям, где ученик был */
    function studentTotal(studentId: string) {
        let solved = 0;
        let total = 0;
        for (const lesson of data.summary?.lessons ?? []) {
            const cell = matrixCell(studentId, lesson.id);
            if (!cell || cell.attendance === "Absent") continue;
            solved += cell.solved;
            total += lesson.taskCount;
        }
        return { solved, total };
    }

    /** Цвет ячейки сводки: чем больше решено, тем насыщеннее */
    function scoreClass(cell: MatrixCell, taskCount: number): string {
        const score = taskCount ? cell.solved / taskCount : 0;
        if (score >= 0.75) return "bg-grade-solved/80 text-white";
        if (score >= 0.5) return "bg-grade-solved/45";
        if (score >= 0.25) return "bg-grade-solved-remarks/40";
        return "bg-grade-unsolved/25";
    }
</script>

<svelte:head><title>{data.group.title} — Кружки 218</title></svelte:head>

<PageHeader
    title={data.group.title}
    description={data.group.description}
    back={{ href: "/teacher", label: "Мои группы" }}
/>

<Tabs.Root value={data.tab} onValueChange={selectTab}>
    <Tabs.List class="mb-4">
        <Tabs.Trigger value="lessons">Занятия</Tabs.Trigger>
        <Tabs.Trigger value="students">
            Ученики
            <span class="text-muted-foreground">
                {data.group.students.length}
            </span>
        </Tabs.Trigger>
        <Tabs.Trigger value="summary">Сводка</Tabs.Trigger>
    </Tabs.List>

    <Tabs.Content value="lessons" class="flex flex-col gap-4">
        <div class="flex flex-wrap gap-2">
            <Button onclick={() => (lessonOpen = true)}>
                <PlusIcon />
                Новое занятие
            </Button>
            <Popover.Root>
                <Popover.Trigger class={buttonVariants({ variant: "outline" })}>
                    <DownloadIcon />
                    Чистый трафарет
                </Popover.Trigger>
                <Popover.Content align="start">
                    <Field.Field>
                        <Field.Label for="template-tasks">
                            Число задач
                        </Field.Label>
                        <Input
                            id="template-tasks"
                            type="number"
                            min={1}
                            max={30}
                            class="w-24"
                            bind:value={templateTasks}
                        />
                        <Field.Description>
                            Таблица со списком группы для печати. Дату занятия
                            впишите в файл перед загрузкой.
                        </Field.Description>
                    </Field.Field>
                    <Button
                        href={groupTemplateUrl(
                            groupId,
                            Math.min(Math.max(templateTasks || 1, 1), 30),
                        )}
                        download
                        data-sveltekit-reload
                    >
                        <DownloadIcon />
                        Скачать
                    </Button>
                </Popover.Content>
            </Popover.Root>
        </div>

        {#if !data.lessons.length}
            <Empty.Root class="border">
                <Empty.Header>
                    <Empty.Title>Занятий пока нет</Empty.Title>
                    <Empty.Description>
                        Создайте занятие или загрузите заполненный трафарет — по
                        дате из файла занятие создастся само.
                    </Empty.Description>
                </Empty.Header>
            </Empty.Root>
        {:else}
            <div class="overflow-x-auto rounded-lg border">
                <Table.Root>
                    <Table.Header>
                        <Table.Row>
                            <Table.Head>Дата</Table.Head>
                            <Table.Head>Тема</Table.Head>
                            <Table.Head class="text-right">Задач</Table.Head>
                            <Table.Head class="w-40">Заполнено</Table.Head>
                            <Table.Head>Статус</Table.Head>
                        </Table.Row>
                    </Table.Header>
                    <Table.Body>
                        {#each data.lessons as lesson (lesson.id)}
                            {@const upcoming = lesson.date > today()}
                            <Table.Row
                                class="relative cursor-pointer"
                                onclick={() =>
                                    goto(`/teacher/lessons/${lesson.id}`)}
                            >
                                <Table.Cell>
                                    <a
                                        href="/teacher/lessons/{lesson.id}"
                                        class="font-medium hover:underline"
                                    >
                                        {formatDate(lesson.date)}
                                    </a>
                                    <span class="text-muted-foreground">
                                        {formatWeekday(lesson.date)}
                                        {formatTime(lesson.time)}
                                    </span>
                                </Table.Cell>
                                <Table.Cell
                                    class="max-w-64 truncate text-muted-foreground"
                                >
                                    {lesson.topic ?? "—"}
                                </Table.Cell>
                                <Table.Cell class="text-right tabular-nums">
                                    {lesson.taskCount}
                                </Table.Cell>
                                <Table.Cell>
                                    <div class="flex items-center gap-2">
                                        <Progress
                                            value={lesson.filledPercent}
                                            class="w-20"
                                            aria-label="Заполненность журнала"
                                        />
                                        <span
                                            class="text-xs text-muted-foreground tabular-nums"
                                        >
                                            {Math.round(lesson.filledPercent)}%
                                        </span>
                                    </div>
                                </Table.Cell>
                                <Table.Cell>
                                    {#if lesson.isPublished}
                                        <Badge>Опубликован</Badge>
                                    {:else if upcoming}
                                        <Badge variant="outline">
                                            Запланировано
                                        </Badge>
                                    {:else}
                                        <Badge variant="secondary">
                                            Черновик
                                        </Badge>
                                    {/if}
                                </Table.Cell>
                            </Table.Row>
                        {/each}
                    </Table.Body>
                </Table.Root>
            </div>
        {/if}
    </Tabs.Content>

    <Tabs.Content value="students" class="flex flex-col gap-4">
        <div>
            <Button onclick={() => (studentOpen = true)}>
                <UserPlusIcon />
                Добавить ученика
            </Button>
        </div>

        {#if !data.group.students.length}
            <Empty.Root class="border">
                <Empty.Header>
                    <Empty.Title>В группе нет учеников</Empty.Title>
                    <Empty.Description>
                        Добавьте учеников вручную — или их добавит
                        администратор.
                    </Empty.Description>
                </Empty.Header>
            </Empty.Root>
        {:else}
            <ul class="divide-y rounded-lg border">
                {#each data.group.students as student (student.id)}
                    <li class="flex items-center gap-3 px-4 py-2">
                        <span class="min-w-0 flex-1 truncate">
                            {fullName(student)}
                        </span>
                        {#if student.grade}
                            <span class="text-sm text-muted-foreground">
                                {student.grade}
                            </span>
                        {/if}
                        <Button
                            variant="ghost"
                            size="icon-sm"
                            title="Убрать из группы"
                            onclick={() => {
                                removing = student;
                                removeOpen = true;
                            }}
                        >
                            <Trash2Icon />
                            <span class="sr-only">Убрать из группы</span>
                        </Button>
                    </li>
                {/each}
            </ul>
        {/if}
    </Tabs.Content>

    <Tabs.Content value="summary">
        {#if data.summary}
            {@const summary = data.summary}
            {#if !summary.lessons.length || !summary.students.length}
                <Empty.Root class="border">
                    <Empty.Header>
                        <Empty.Title>Пока нечего сводить</Empty.Title>
                        <Empty.Description>
                            Здесь появится таблица «ученики × занятия», когда
                            будут заполнены журналы.
                        </Empty.Description>
                    </Empty.Header>
                </Empty.Root>
            {:else}
                <div class="w-fit max-w-full overflow-x-auto rounded-lg border">
                    <table
                        class="w-max border-separate border-spacing-0 text-sm"
                    >
                        <caption class="sr-only">
                            Решённые задачи по занятиям
                        </caption>
                        <thead>
                            <tr>
                                <th
                                    scope="col"
                                    class="sticky left-0 z-10 min-w-36 sm:min-w-48 border-b bg-background px-3 py-2 text-left font-medium"
                                >
                                    Ученик
                                </th>
                                <th
                                    scope="col"
                                    class="border-b border-l px-2 py-2 font-medium"
                                >
                                    Всего
                                </th>
                                {#each summary.lessons as lesson (lesson.id)}
                                    <th
                                        scope="col"
                                        class="border-b border-l px-1 py-2 font-medium"
                                        title="{lesson.topic ??
                                            ''}{lesson.isPublished
                                            ? ''
                                            : ' (не опубликован)'}"
                                    >
                                        <a
                                            href="/teacher/lessons/{lesson.id}"
                                            class={cn(
                                                "hover:underline",
                                                !lesson.isPublished &&
                                                    "text-muted-foreground italic",
                                            )}
                                        >
                                            {formatShortDate(lesson.date)}
                                        </a>
                                    </th>
                                {/each}
                            </tr>
                        </thead>
                        <tbody>
                            {#each summary.students as student (student.id)}
                                {@const total = studentTotal(student.id)}
                                <tr
                                    class={student.inGroup
                                        ? undefined
                                        : "text-muted-foreground"}
                                >
                                    <th
                                        scope="row"
                                        class="sticky left-0 z-10 border-b bg-background px-3 py-1.5 text-left font-normal"
                                    >
                                        {shortName(student)}
                                        {#if !student.inGroup}
                                            <span class="text-xs">(выбыл)</span>
                                        {/if}
                                    </th>
                                    <td
                                        class="border-b border-l px-2 text-center whitespace-nowrap tabular-nums"
                                    >
                                        {#if total.total}
                                            {total.solved} из {total.total}
                                        {:else}
                                            —
                                        {/if}
                                    </td>
                                    {#each summary.lessons as lesson (lesson.id)}
                                        {@const cell = matrixCell(
                                            student.id,
                                            lesson.id,
                                        )}
                                        <td class="border-b border-l p-0.5">
                                            {#if cell?.attendance === "Absent"}
                                                <span
                                                    class="flex h-7 min-w-11 items-center justify-center text-xs text-muted-foreground"
                                                    title="Не был на занятии"
                                                >
                                                    н
                                                </span>
                                            {:else if cell}
                                                {@const late =
                                                    cell.attendance === "Late"}
                                                <span
                                                    class={cn(
                                                        "relative flex h-7 min-w-11 items-center justify-center rounded text-xs tabular-nums",
                                                        scoreClass(
                                                            cell,
                                                            lesson.taskCount,
                                                        ),
                                                    )}
                                                    title="{late
                                                        ? 'Опоздал. '
                                                        : ''}Решил {cell.solved} из {lesson.taskCount}"
                                                >
                                                    {cell.solved}/{lesson.taskCount}
                                                    {#if late}
                                                        <span
                                                            class="absolute top-0.5 right-0.5 size-1.5 rounded-full bg-amber-500"
                                                        ></span>
                                                    {/if}
                                                </span>
                                            {:else}
                                                <span
                                                    class="flex h-7 min-w-11 items-center justify-center text-xs text-muted-foreground/50"
                                                    title="Нет отметок"
                                                >
                                                    ·
                                                </span>
                                            {/if}
                                        </td>
                                    {/each}
                                </tr>
                            {/each}
                        </tbody>
                    </table>
                </div>
                <p class="mt-2 text-xs text-muted-foreground">
                    Решённые задачи из заданных; «н» — не был, жёлтая точка —
                    опоздал, «·» — нет отметок.
                    Курсивом — неопубликованные занятия.
                </p>
            {/if}
        {/if}
    </Tabs.Content>
</Tabs.Root>

<LessonDialog
    bind:open={lessonOpen}
    mode={{ kind: "create", groupId, taskCount: lastTaskCount }}
    onSaved={(lesson) => goto(`/teacher/lessons/${lesson.id}`)}
/>

<StudentDialog bind:open={studentOpen} onSubmit={addStudent} />

<ConfirmDialog
    bind:open={removeOpen}
    title="Убрать из группы?"
    description={removing
        ? `${fullName(removing)} больше не будет в новых журналах группы. Уже выставленные отметки сохранятся.`
        : undefined}
    confirmText="Убрать"
    destructive
    onConfirm={removeStudent}
/>
