<script lang="ts" module>
    export type SaveState = "saved" | "dirty" | "saving" | "error";
</script>

<script lang="ts">
    import { tick, untrack } from "svelte";
    import { SvelteMap } from "svelte/reactivity";
    import { beforeNavigate } from "$app/navigation";
    import CheckIcon from "@lucide/svelte/icons/check";
    import CircleAlertIcon from "@lucide/svelte/icons/circle-alert";
    import EllipsisVerticalIcon from "@lucide/svelte/icons/ellipsis-vertical";
    import MessageSquareIcon from "@lucide/svelte/icons/message-square";
    import PencilIcon from "@lucide/svelte/icons/pencil";
    import PlusIcon from "@lucide/svelte/icons/plus";
    import Trash2Icon from "@lucide/svelte/icons/trash-2";
    import UserXIcon from "@lucide/svelte/icons/user-x";
    import { errorMessage } from "$lib/api/client";
    import {
        saveGradebook,
        type GradebookCellChange,
        type GradebookCellDto,
    } from "$lib/api/teacher";
    import type {
        GradebookEntryTaskStatus,
        StudentDto,
        TaskDto,
    } from "$lib/api/types";

    type GridStudent = StudentDto & { inGroup?: boolean };
    import { Button } from "$lib/components/ui/button";
    import * as DropdownMenu from "$lib/components/ui/dropdown-menu";
    import * as Popover from "$lib/components/ui/popover";
    import { Textarea } from "$lib/components/ui/textarea";
    import { shortName } from "$lib/format";
    import {
        GRADE_LIST,
        GRADES,
        gradeByKey,
        isSolved,
        nextGrade,
    } from "$lib/grades";
    import { cn } from "$lib/utils";

    let {
        lessonId,
        students,
        tasks,
        entries,
        saveState = $bindable("saved"),
        onAddTask,
        onRenameTask,
        onDeleteTask,
    }: {
        lessonId: string;
        students: GridStudent[];
        tasks: TaskDto[];
        entries: GradebookCellDto[];
        saveState?: SaveState;
        onAddTask?: () => void;
        onRenameTask?: (task: TaskDto) => void;
        onDeleteTask?: (task: TaskDto, marks: number) => void;
    } = $props();

    const DEBOUNCE_MS = 1500;
    const COMMENT_MAX = 500;

    type Cell = { status: GradebookEntryTaskStatus; comment: string | null };

    const key = (studentId: string, taskId: string) => `${studentId}:${taskId}`;

    /** Текущее состояние ячеек (сервер + несохранённые правки) */
    const cells = new SvelteMap<string, Cell>();
    /** Несохранённые изменения: null — удалить отметку */
    // очередь на отправку, разметка от неё не зависит
    // eslint-disable-next-line svelte/prefer-svelte-reactivity
    const dirty = new Map<string, Cell | null>();

    // При перезагрузке данных (добавили задачу, импорт) не теряем свои правки
    $effect.pre(() => {
        const fresh = entries;
        untrack(() => {
            cells.clear();
            for (const entry of fresh) {
                cells.set(key(entry.studentId, entry.taskId), {
                    status: entry.status,
                    comment: entry.comment,
                });
            }
            for (const [cellKey, value] of dirty) {
                if (value) cells.set(cellKey, value);
                else cells.delete(cellKey);
            }
        });
    });

    let timer: ReturnType<typeof setTimeout> | undefined;
    let inFlight: Promise<void> | null = null;

    function setCell(studentId: string, taskId: string, value: Cell | null) {
        const cellKey = key(studentId, taskId);
        if (value) cells.set(cellKey, value);
        else cells.delete(cellKey);
        dirty.set(cellKey, value);
        saveState = "dirty";
        clearTimeout(timer);
        timer = setTimeout(() => void flush(), DEBOUNCE_MS);
    }

    /** Отправить все несохранённые изменения одним PUT. */
    export async function flush(): Promise<void> {
        clearTimeout(timer);
        if (inFlight) await inFlight;
        if (!dirty.size) return;

        const batch = new Map(dirty);
        dirty.clear();
        const changes: GradebookCellChange[] = [...batch].map(
            ([cellKey, value]) => {
                const [studentId, taskId] = cellKey.split(":");
                return {
                    studentId,
                    taskId,
                    status: value?.status ?? null,
                    comment: value?.comment ?? null,
                };
            },
        );

        saveState = "saving";
        inFlight = saveGradebook(lessonId, changes)
            .then(() => {
                saveState = dirty.size ? "dirty" : "saved";
            })
            .catch((error) => {
                // вернуть в очередь то, что не перезаписали за время запроса
                for (const [cellKey, value] of batch) {
                    if (!dirty.has(cellKey)) dirty.set(cellKey, value);
                }
                saveState = "error";
                saveError = errorMessage(error);
                throw error;
            })
            .finally(() => {
                inFlight = null;
            });
        await inFlight;
    }

    let saveError = $state<string | null>(null);

    export function hasPendingChanges(): boolean {
        return dirty.size > 0 || inFlight !== null;
    }

    function rowMarks(studentId: string): number {
        return tasks.filter((task) => cells.has(key(studentId, task.id)))
            .length;
    }

    /** Строки, где часть задач отмечена, а часть нет */
    export function incompleteRows(): number {
        return students.filter((student) => {
            const marks = rowMarks(student.id);
            return marks > 0 && marks < tasks.length;
        }).length;
    }

    function taskMarks(taskId: string): number {
        return students.filter((student) => cells.has(key(student.id, taskId)))
            .length;
    }

    beforeNavigate(({ type, cancel }) => {
        if (!hasPendingChanges()) return;
        // закрытие вкладки: браузер покажет своё подтверждение
        if (type === "leave") cancel();
        else void flush().catch(() => {});
    });

    // ---------- фокус и клавиатура ----------

    let focusRow = $state(0);
    let focusCol = $state(0);
    let table = $state<HTMLTableElement | null>(null);

    const focused = $derived(
        students[focusRow] && tasks[focusCol]
            ? { student: students[focusRow], task: tasks[focusCol] }
            : null,
    );
    const focusedCell = $derived(
        focused
            ? cells.get(key(focused.student.id, focused.task.id))
            : undefined,
    );

    function cellElement(row: number, col: number): HTMLElement | null {
        return (
            table?.querySelector<HTMLElement>(
                `[data-row="${row}"][data-col="${col}"]`,
            ) ?? null
        );
    }

    async function moveTo(row: number, col: number) {
        focusRow = Math.min(Math.max(row, 0), students.length - 1);
        focusCol = Math.min(Math.max(col, 0), tasks.length - 1);
        await tick();
        cellElement(focusRow, focusCol)?.focus();
    }

    /** Следующая ячейка: вправо, в конце строки — на начало следующей */
    function advance() {
        if (focusCol < tasks.length - 1) void moveTo(focusRow, focusCol + 1);
        else if (focusRow < students.length - 1) void moveTo(focusRow + 1, 0);
    }

    function setStatus(
        row: number,
        col: number,
        status: GradebookEntryTaskStatus | null,
    ) {
        const student = students[row];
        const task = tasks[col];
        const current = cells.get(key(student.id, task.id));
        setCell(
            student.id,
            task.id,
            status ? { status, comment: current?.comment ?? null } : null,
        );
    }

    function cycle(row: number, col: number) {
        const current = cells.get(key(students[row].id, tasks[col].id));
        setStatus(row, col, nextGrade(current?.status ?? null));
    }

    function onCellKeydown(event: KeyboardEvent, row: number, col: number) {
        if (event.ctrlKey || event.metaKey || event.altKey) return;
        switch (event.key) {
            case "ArrowLeft":
                void moveTo(row, col - 1);
                break;
            case "ArrowRight":
                void moveTo(row, col + 1);
                break;
            case "ArrowUp":
                void moveTo(row - 1, col);
                break;
            case "ArrowDown":
                void moveTo(row + 1, col);
                break;
            case "Home":
                void moveTo(row, 0);
                break;
            case "End":
                void moveTo(row, tasks.length - 1);
                break;
            case " ":
            case "Enter":
                cycle(row, col);
                break;
            case "c":
            case "C":
            case "с":
            case "С":
                openComment();
                break;
            default: {
                const status = gradeByKey(event.key);
                if (status === undefined) return;
                setStatus(row, col, status);
                if (status !== null) advance();
            }
        }
        event.preventDefault();
    }

    function onCellClick(row: number, col: number) {
        focusRow = row;
        focusCol = col;
        cycle(row, col);
    }

    // ---------- строка целиком ----------

    function fillRow(studentId: string) {
        for (const task of tasks) {
            const current = cells.get(key(studentId, task.id));
            setCell(studentId, task.id, {
                status: "Solved",
                comment: current?.comment ?? null,
            });
        }
    }

    function clearRow(studentId: string) {
        for (const task of tasks) {
            if (cells.has(key(studentId, task.id)))
                setCell(studentId, task.id, null);
        }
    }

    // ---------- комментарий ----------

    let commentOpen = $state(false);
    let commentText = $state("");
    let commentAnchor = $state<HTMLElement | null>(null);
    let commentTarget: { row: number; col: number } | null = null;

    function openComment() {
        if (!focused || !focusedCell) return;
        commentTarget = { row: focusRow, col: focusCol };
        commentAnchor = cellElement(focusRow, focusCol);
        commentText = focusedCell.comment ?? "";
        commentOpen = true;
    }

    function saveComment(event: SubmitEvent) {
        event.preventDefault();
        if (commentTarget) {
            const student = students[commentTarget.row];
            const task = tasks[commentTarget.col];
            const current = cells.get(key(student.id, task.id));
            if (current) {
                const comment = commentText.trim() || null;
                if (comment !== current.comment)
                    setCell(student.id, task.id, { ...current, comment });
            }
        }
        commentOpen = false;
    }

    function onCommentOpenChange(open: boolean) {
        if (!open && commentTarget)
            void moveTo(commentTarget.row, commentTarget.col);
    }

    function onCommentKeydown(event: KeyboardEvent) {
        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
            (event.currentTarget as HTMLTextAreaElement).form?.requestSubmit();
        }
    }
</script>

<div class="flex flex-col gap-3">
    <!-- Палитра: те же действия, что клавиши, — для мыши и телефона -->
    <div
        class="flex flex-wrap items-center gap-2 rounded-lg border bg-muted/30 px-3 py-2"
    >
        <span class="min-w-0 flex-1 truncate text-sm">
            {#if focused}
                <span class="font-medium">{shortName(focused.student)}</span>
                <span class="text-muted-foreground">
                    · задача {focused.task.order}{focused.task.title
                        ? ` «${focused.task.title}»`
                        : ""}
                </span>
            {:else}
                <span class="text-muted-foreground">Выберите ячейку</span>
            {/if}
        </span>
        <div class="flex flex-wrap items-center gap-1">
            {#each GRADE_LIST as grade (grade.status)}
                <Button
                    variant="outline"
                    size="sm"
                    disabled={!focused}
                    title="{grade.label} (клавиша {grade.key})"
                    onclick={() => {
                        setStatus(focusRow, focusCol, grade.status);
                        advance();
                    }}
                >
                    <span
                        class={cn(
                            "inline-flex h-5 min-w-6 items-center justify-center rounded px-1 font-mono text-xs font-semibold",
                            grade.className,
                        )}>{grade.symbol}</span
                    >
                    <kbd class="text-xs text-muted-foreground">{grade.key}</kbd>
                </Button>
            {/each}
            <Button
                variant="outline"
                size="sm"
                disabled={!focusedCell}
                title="Очистить (клавиша 0 или Del)"
                onclick={() => {
                    setStatus(focusRow, focusCol, null);
                    void moveTo(focusRow, focusCol);
                }}
            >
                ·
                <kbd class="text-xs text-muted-foreground">0</kbd>
            </Button>
            <Button
                variant="outline"
                size="sm"
                disabled={!focusedCell}
                title="Комментарий (клавиша C)"
                onclick={openComment}
            >
                <MessageSquareIcon />
                <kbd class="text-xs text-muted-foreground">C</kbd>
            </Button>
        </div>
    </div>

    <div class="w-fit max-w-full overflow-x-auto rounded-lg border">
        <table
            bind:this={table}
            class="w-max border-separate border-spacing-0 text-sm"
            role="grid"
            aria-label="Журнал занятия"
        >
            <thead>
                <tr>
                    <th
                        scope="col"
                        class="sticky left-0 z-20 min-w-36 sm:min-w-48 border-b bg-background px-3 py-2 text-left font-medium"
                    >
                        Ученик
                    </th>
                    {#each tasks as task (task.id)}
                        <th
                            scope="col"
                            class="border-b border-l px-1 py-1 font-medium"
                        >
                            <DropdownMenu.Root>
                                <DropdownMenu.Trigger>
                                    {#snippet child({ props })}
                                        <button
                                            {...props}
                                            class="flex w-full min-w-11 flex-col items-center rounded px-1 py-0.5 hover:bg-muted"
                                            title={task.title ?? undefined}
                                        >
                                            <span>{task.order}</span>
                                            {#if task.title}
                                                <span
                                                    class="max-w-20 truncate text-xs font-normal text-muted-foreground"
                                                >
                                                    {task.title}
                                                </span>
                                            {/if}
                                        </button>
                                    {/snippet}
                                </DropdownMenu.Trigger>
                                <DropdownMenu.Content align="center">
                                    <DropdownMenu.Label>
                                        Задача {task.order}
                                    </DropdownMenu.Label>
                                    <DropdownMenu.Item
                                        onSelect={() => onRenameTask?.(task)}
                                    >
                                        <PencilIcon />
                                        Название
                                    </DropdownMenu.Item>
                                    <DropdownMenu.Item
                                        variant="destructive"
                                        disabled={tasks.length <= 1}
                                        onSelect={() =>
                                            onDeleteTask?.(
                                                task,
                                                taskMarks(task.id),
                                            )}
                                    >
                                        <Trash2Icon />
                                        Удалить
                                    </DropdownMenu.Item>
                                </DropdownMenu.Content>
                            </DropdownMenu.Root>
                        </th>
                    {/each}
                    <th scope="col" class="border-b border-l px-1 py-1">
                        {#if onAddTask}
                            <Button
                                variant="ghost"
                                size="icon-sm"
                                title="Добавить задачу"
                                onclick={onAddTask}
                            >
                                <PlusIcon />
                                <span class="sr-only">Добавить задачу</span>
                            </Button>
                        {/if}
                    </th>
                </tr>
            </thead>
            <tbody>
                {#each students as student, row (student.id)}
                    {@const marks = rowMarks(student.id)}
                    <tr class="group">
                        <th
                            scope="row"
                            class="sticky left-0 z-10 border-b bg-background px-1 py-0 text-left font-normal group-hover:bg-muted"
                        >
                            <div class="flex items-center gap-1">
                                <span class="min-w-0 flex-1 truncate pl-2">
                                    {shortName(student)}
                                    {#if student.inGroup === false}
                                        <span
                                            class="text-xs text-muted-foreground"
                                            title="Ученик выбыл из группы, но отметки на этом занятии остались"
                                        >
                                            (выбыл)
                                        </span>
                                    {/if}
                                </span>
                                {#if marks > 0 && marks < tasks.length}
                                    <span
                                        class="size-1.5 shrink-0 rounded-full bg-amber-500"
                                        title="Строка заполнена не полностью"
                                    ></span>
                                {/if}
                                <DropdownMenu.Root>
                                    <DropdownMenu.Trigger>
                                        {#snippet child({ props })}
                                            <Button
                                                {...props}
                                                variant="ghost"
                                                size="icon-sm"
                                                class="shrink-0 text-muted-foreground"
                                            >
                                                <EllipsisVerticalIcon />
                                                <span class="sr-only">
                                                    Действия со строкой
                                                </span>
                                            </Button>
                                        {/snippet}
                                    </DropdownMenu.Trigger>
                                    <DropdownMenu.Content align="start">
                                        <DropdownMenu.Item
                                            onSelect={() => fillRow(student.id)}
                                        >
                                            <CheckIcon />
                                            Решил все задачи
                                        </DropdownMenu.Item>
                                        <DropdownMenu.Item
                                            disabled={marks === 0}
                                            onSelect={() =>
                                                clearRow(student.id)}
                                        >
                                            <UserXIcon />
                                            Не был (очистить строку)
                                        </DropdownMenu.Item>
                                    </DropdownMenu.Content>
                                </DropdownMenu.Root>
                            </div>
                        </th>
                        {#each tasks as task, col (task.id)}
                            {@const cell = cells.get(key(student.id, task.id))}
                            {@const grade = cell ? GRADES[cell.status] : null}
                            <td
                                class="border-b border-l p-0.5"
                                role="gridcell"
                                aria-selected={row === focusRow &&
                                    col === focusCol}
                            >
                                <button
                                    type="button"
                                    data-row={row}
                                    data-col={col}
                                    tabindex={row === focusRow &&
                                    col === focusCol
                                        ? 0
                                        : -1}
                                    class={cn(
                                        "relative flex h-9 w-full min-w-11 items-center justify-center rounded font-mono font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1",
                                        grade?.className ??
                                            "text-muted-foreground/40 hover:bg-muted",
                                    )}
                                    aria-label="{shortName(
                                        student,
                                    )}, задача {task.order}: {grade?.label ??
                                        'нет отметки'}{cell?.comment
                                        ? `, комментарий: ${cell.comment}`
                                        : ''}"
                                    title={cell?.comment ?? undefined}
                                    onfocus={() => {
                                        focusRow = row;
                                        focusCol = col;
                                    }}
                                    onclick={() => onCellClick(row, col)}
                                    onkeydown={(event) =>
                                        onCellKeydown(event, row, col)}
                                >
                                    {grade?.symbol ?? "·"}
                                    {#if cell?.comment}
                                        <span
                                            class="absolute top-0.5 right-0.5 size-0 border-t-[6px] border-l-[6px] border-t-current border-l-transparent opacity-70"
                                        ></span>
                                    {/if}
                                </button>
                            </td>
                        {/each}
                        <td class="border-b border-l"></td>
                    </tr>
                {/each}
            </tbody>
            <tfoot>
                <tr class="text-xs text-muted-foreground">
                    <th
                        scope="row"
                        class="sticky left-0 z-10 bg-background px-3 py-2 text-left font-normal"
                    >
                        Решили
                    </th>
                    {#each tasks as task (task.id)}
                        {@const present = taskMarks(task.id)}
                        {@const solved = students.filter((student) =>
                            isSolved(
                                cells.get(key(student.id, task.id))?.status ??
                                    null,
                            ),
                        ).length}
                        <td class="border-l px-1 py-2 text-center">
                            {#if present}{solved} из {present}{:else}—{/if}
                        </td>
                    {/each}
                    <td class="border-l"></td>
                </tr>
            </tfoot>
        </table>
    </div>

    <p class="text-xs text-muted-foreground">
        Клик по ячейке перебирает отметки. С клавиатуры: стрелки — переход, 1–4
        — отметка и сдвиг вправо, 0 или Del — очистить, C — комментарий.
        Изменения сохраняются автоматически.
    </p>

    {#if saveState === "error"}
        <div
            class="flex flex-wrap items-center gap-2 text-sm text-destructive"
            role="alert"
        >
            <CircleAlertIcon class="size-4" />
            Не удалось сохранить: {saveError}
            <Button
                variant="outline"
                size="sm"
                onclick={() => void flush().catch(() => {})}
            >
                Повторить
            </Button>
        </div>
    {/if}
</div>

<Popover.Root bind:open={commentOpen} onOpenChange={onCommentOpenChange}>
    <Popover.Content customAnchor={commentAnchor} side="bottom" class="w-80">
        <form class="flex flex-col gap-3" onsubmit={saveComment}>
            <label for="grade-comment" class="text-sm font-medium">
                Комментарий к отметке
            </label>
            <Textarea
                id="grade-comment"
                bind:value={commentText}
                maxlength={COMMENT_MAX}
                rows={3}
                placeholder="Его увидят родители ученика"
                onkeydown={onCommentKeydown}
            />
            <div class="flex items-center justify-between gap-2">
                <span class="text-xs text-muted-foreground">
                    {commentText.length}/{COMMENT_MAX} · Ctrl+Enter
                </span>
                <div class="flex gap-2">
                    <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onclick={() => (commentOpen = false)}
                    >
                        Отмена
                    </Button>
                    <Button type="submit" size="sm">Сохранить</Button>
                </div>
            </div>
        </form>
    </Popover.Content>
</Popover.Root>

<svelte:window
    onbeforeunload={(event) => {
        if (hasPendingChanges()) event.preventDefault();
    }}
/>
