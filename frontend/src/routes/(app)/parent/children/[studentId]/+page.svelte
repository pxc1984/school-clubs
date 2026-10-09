<script lang="ts">
    import FlameIcon from "@lucide/svelte/icons/flame";
    import TrophyIcon from "@lucide/svelte/icons/trophy";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import GradeChip from "$lib/components/grade-chip.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import ProgressChart from "$lib/components/progress-chart.svelte";
    import * as Accordion from "$lib/components/ui/accordion";
    import { Badge } from "$lib/components/ui/badge";
    import * as Empty from "$lib/components/ui/empty";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import * as ToggleGroup from "$lib/components/ui/toggle-group";
    import {
        countOf,
        formatDate,
        formatDecimal,
        formatPercent,
        formatWeekday,
        fullName,
    } from "$lib/format";
    import { GRADES } from "$lib/grades";
    import { POSITION_LABELS, PERIODS, type PeriodKey } from "$lib/stats";

    let { data } = $props();

    function navigate(changes: Record<string, string>) {
        const url = new URL(page.url);
        for (const [key, value] of Object.entries(changes))
            url.searchParams.set(key, value);
        goto(url, { replaceState: true, noScroll: true, keepFocus: true });
    }
</script>

<svelte:head>
    <title>{fullName(data.child)} — Кружки 218</title>
</svelte:head>

<div class="mx-auto max-w-2xl">
    <PageHeader
        title={fullName(data.child)}
        description={data.child.grade ? `${data.child.grade} класс` : null}
        back={{ href: "/parent", label: "Мои дети" }}
    />

    {#if !data.group}
        <Empty.Root class="border">
            <Empty.Header>
                <Empty.Title>Ребёнок пока не записан в кружки</Empty.Title>
            </Empty.Header>
        </Empty.Root>
    {:else}
        {@const summary = data.summary!}
        <div class="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
            {#if data.child.groups.length > 1}
                <NativeSelect.Root
                    class="sm:w-64"
                    value={data.group.id}
                    aria-label="Кружок"
                    onchange={(event) =>
                        navigate({ group: event.currentTarget.value })}
                >
                    {#each data.child.groups as group (group.id)}
                        <NativeSelect.Option value={group.id}>
                            {group.title}
                        </NativeSelect.Option>
                    {/each}
                </NativeSelect.Root>
            {:else}
                <span class="font-medium">{data.group.title}</span>
            {/if}
            <ToggleGroup.Root
                type="single"
                variant="outline"
                size="sm"
                value={data.periodKey}
                onValueChange={(value) => {
                    if (value) navigate({ period: value as PeriodKey });
                }}
                aria-label="Период"
                class="sm:ml-auto"
            >
                {#each PERIODS as period (period.key)}
                    <ToggleGroup.Item value={period.key}>
                        {period.label}
                    </ToggleGroup.Item>
                {/each}
            </ToggleGroup.Root>
        </div>

        <!-- главная цифра наверху -->
        <section class="mb-6">
            {#if summary.tasksTotal}
                <p class="text-4xl font-semibold tracking-tight">
                    {summary.tasksSolved}
                    <span class="text-xl font-normal text-muted-foreground">
                        из {countOf(summary.tasksTotal, [
                            "задачи",
                            "задач",
                            "задач",
                        ])} решено
                    </span>
                </p>
            {:else}
                <p class="text-lg text-muted-foreground">
                    За этот период отметок нет
                </p>
            {/if}
            <div class="mt-3 flex flex-wrap gap-2">
                {#if summary.position}
                    <Badge>
                        <TrophyIcon />
                        {POSITION_LABELS[summary.position]}
                    </Badge>
                {/if}
                {#if summary.perfectStreak}
                    <Badge variant="secondary">
                        <FlameIcon />
                        {countOf(summary.perfectStreak, [
                            "занятие",
                            "занятия",
                            "занятий",
                        ])} подряд решил всё
                    </Badge>
                {/if}
            </div>
            <dl class="mt-4 grid grid-cols-2 gap-4 text-sm">
                <div>
                    <dt class="text-muted-foreground">Посещаемость</dt>
                    <dd class="font-medium">
                        Был на {summary.lessonsAttended} из {countOf(
                            summary.lessonsTotal,
                            ["занятия", "занятий", "занятий"],
                        )}
                    </dd>
                </div>
                {#if summary.score !== null}
                    <div>
                        <dt class="text-muted-foreground">Средний балл</dt>
                        <dd class="font-medium">
                            {formatPercent(summary.score)}
                        </dd>
                    </div>
                {/if}
            </dl>
        </section>

        {#if summary.chart.length > 1}
            <section class="mb-8">
                <h2 class="mb-3 text-lg font-semibold">Динамика</h2>
                <ProgressChart series={summary.chart} />
            </section>
        {/if}

        <section>
            <h2 class="mb-1 text-lg font-semibold">Занятия</h2>
            {#if !data.lessons.length}
                <p class="text-sm text-muted-foreground">
                    Опубликованных занятий за этот период нет.
                </p>
            {:else}
                <Accordion.Root type="multiple">
                    {#each data.lessons as lesson (lesson.lessonId)}
                        <Accordion.Item value={lesson.lessonId}>
                            <Accordion.Trigger class="items-center">
                                <div
                                    class="flex min-w-0 flex-1 items-center gap-3"
                                >
                                    <div class="w-16 shrink-0 text-sm">
                                        <div class="font-medium">
                                            {formatDate(lesson.date)}
                                        </div>
                                        <div class="text-muted-foreground">
                                            {formatWeekday(lesson.date)}
                                        </div>
                                    </div>
                                    <div class="min-w-0 flex-1">
                                        <div class="truncate font-normal">
                                            {lesson.topic ?? "Занятие"}
                                        </div>
                                        <div
                                            class="flex gap-1 pt-1"
                                            aria-hidden="true"
                                        >
                                            {#each lesson.tasks as task (task.taskId)}
                                                <span
                                                    class="size-2.5 rounded-full {task.status
                                                        ? GRADES[task.status]
                                                              .className
                                                        : 'bg-muted'}"
                                                ></span>
                                            {/each}
                                        </div>
                                    </div>
                                    <div
                                        class="shrink-0 text-right text-sm font-medium"
                                    >
                                        {#if lesson.attended}
                                            {lesson.solved} из {lesson.taskCount}
                                            {#if lesson.attendance === "Late"}
                                                <div
                                                    class="text-xs font-normal text-amber-700 dark:text-amber-400"
                                                >
                                                    опоздал
                                                </div>
                                            {/if}
                                        {:else}
                                            <span
                                                class="font-normal text-muted-foreground"
                                                >не был</span
                                            >
                                        {/if}
                                    </div>
                                </div>
                            </Accordion.Trigger>
                            <Accordion.Content>
                                {#if !lesson.attended}
                                    <p class="text-muted-foreground">
                                        Не был на занятии
                                    </p>
                                {:else}
                                    {#if lesson.attendance === "Late"}
                                        <p class="mb-3 text-muted-foreground">
                                            Опоздал на занятие.
                                        </p>
                                    {/if}
                                    {#if lesson.groupAverageSolved !== null}
                                        <p class="mb-3 text-muted-foreground">
                                            Решил {lesson.solved} из {lesson.taskCount},
                                            в среднем по группе — {formatDecimal(
                                                lesson.groupAverageSolved,
                                            )}
                                        </p>
                                    {/if}
                                    <ul class="flex flex-col gap-3">
                                        {#each lesson.tasks as task (task.taskId)}
                                            <li class="flex gap-3">
                                                <GradeChip
                                                    status={task.status}
                                                />
                                                <div class="min-w-0 flex-1">
                                                    <div>
                                                        Задача {task.number}{task.title
                                                            ? `: ${task.title}`
                                                            : ""}
                                                    </div>
                                                    {#if task.status}
                                                        <div
                                                            class="text-xs text-muted-foreground"
                                                        >
                                                            {GRADES[task.status]
                                                                .label}
                                                        </div>
                                                    {/if}
                                                    {#if task.comment}
                                                        <blockquote
                                                            class="mt-1 border-l-2 pl-2 text-sm italic"
                                                        >
                                                            {task.comment}
                                                        </blockquote>
                                                    {/if}
                                                    {#if task.groupSolved !== null && task.groupAttended !== null}
                                                        <div
                                                            class="mt-0.5 text-xs text-muted-foreground"
                                                        >
                                                            Решили {task.groupSolved}
                                                            из {task.groupAttended}
                                                        </div>
                                                    {/if}
                                                </div>
                                            </li>
                                        {/each}
                                    </ul>
                                {/if}
                            </Accordion.Content>
                        </Accordion.Item>
                    {/each}
                </Accordion.Root>
            {/if}
        </section>
    {/if}
</div>
