<script lang="ts">
    import ChevronRightIcon from "@lucide/svelte/icons/chevron-right";
    import TriangleAlertIcon from "@lucide/svelte/icons/triangle-alert";
    import UsersIcon from "@lucide/svelte/icons/users";
    import type { DateOnly } from "$lib/api/types";
    import PageHeader from "$lib/components/page-header.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import * as Empty from "$lib/components/ui/empty";
    import { countOf, formatDate, formatWeekday } from "$lib/format";

    let { data } = $props();

    function lessonLabel(date: DateOnly): string {
        return `${formatWeekday(date)}, ${formatDate(date)}`;
    }
</script>

<svelte:head><title>Мои группы — Кружки 218</title></svelte:head>

<div class="mx-auto max-w-3xl">
    <PageHeader title="Мои группы" />

    {#if !data.groups.length}
        <Empty.Root class="border">
            <Empty.Header>
                <Empty.Media variant="icon"><UsersIcon /></Empty.Media>
                <Empty.Title>Групп пока нет</Empty.Title>
                <Empty.Description>
                    Администратор кружков назначит вас преподавателем группы —
                    она появится здесь.
                </Empty.Description>
            </Empty.Header>
        </Empty.Root>
    {:else}
        <ul class="divide-y rounded-xl border">
            {#each data.groups as group (group.id)}
                <li>
                    <a
                        href="/teacher/groups/{group.id}"
                        class="flex items-center gap-3 px-4 py-4 hover:bg-muted/50"
                    >
                        <div class="min-w-0 flex-1">
                            <div class="flex flex-wrap items-center gap-2">
                                <span class="font-medium">{group.title}</span>
                                <span class="text-sm text-muted-foreground">
                                    {countOf(group.studentCount, [
                                        "ученик",
                                        "ученика",
                                        "учеников",
                                    ])}
                                </span>
                                {#if group.unfilledLessonCount}
                                    <Badge
                                        variant="outline"
                                        class="border-amber-500/50 text-amber-700 dark:text-amber-400"
                                    >
                                        <TriangleAlertIcon />
                                        {countOf(group.unfilledLessonCount, [
                                            "журнал не заполнен",
                                            "журнала не заполнены",
                                            "журналов не заполнены",
                                        ])}
                                    </Badge>
                                {/if}
                            </div>
                            <dl
                                class="mt-1 flex flex-wrap gap-x-4 text-sm text-muted-foreground"
                            >
                                {#if group.nextLessonDate}
                                    <div class="flex gap-1">
                                        <dt>Следующее:</dt>
                                        <dd class="text-foreground">
                                            {lessonLabel(group.nextLessonDate)}
                                        </dd>
                                    </div>
                                {/if}
                                {#if group.lastLessonDate}
                                    <div class="flex gap-1">
                                        <dt>Последнее:</dt>
                                        <dd>
                                            {lessonLabel(group.lastLessonDate)}
                                        </dd>
                                    </div>
                                {/if}
                                {#if !group.nextLessonDate && !group.lastLessonDate}
                                    <dd>Занятий пока нет</dd>
                                {/if}
                            </dl>
                        </div>
                        <ChevronRightIcon
                            class="size-5 shrink-0 text-muted-foreground"
                        />
                    </a>
                </li>
            {/each}
        </ul>
    {/if}
</div>
