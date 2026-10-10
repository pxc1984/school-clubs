<script lang="ts">
    import ChevronRightIcon from "@lucide/svelte/icons/chevron-right";
    import TrophyIcon from "@lucide/svelte/icons/trophy";
    import UserPlusIcon from "@lucide/svelte/icons/user-plus";
    import PageHeader from "$lib/components/page-header.svelte";
    import RequestStatusBadge from "$lib/components/request-status-badge.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Empty from "$lib/components/ui/empty";
    import { formatDate, formatDecimal, fullName } from "$lib/format";
    import { POSITION_LABELS } from "$lib/stats";

    let { data } = $props();
</script>

<svelte:head><title>Мои дети — Кружки 218</title></svelte:head>

<div class="mx-auto max-w-2xl">
    <PageHeader title="Мои дети">
        {#snippet actions()}
            {#if data.children.length}
                <Button href="/parent/requests" variant="outline" size="sm">
                    <UserPlusIcon />
                    Добавить ребёнка
                </Button>
            {/if}
        {/snippet}
    </PageHeader>

    {#if data.pending.length}
        <section class="mb-6 rounded-lg border border-dashed p-4">
            <h2 class="mb-3 text-sm font-medium">Запросы в ожидании</h2>
            <ul class="flex flex-col gap-2">
                {#each data.pending as request (request.id)}
                    <li class="flex flex-wrap items-center gap-2 text-sm">
                        <span class="font-medium">
                            {request.childLastName}
                            {request.childFirstName}
                        </span>
                        <span class="text-muted-foreground">
                            · {request.group.title}
                        </span>
                        <RequestStatusBadge status={request.status} />
                    </li>
                {/each}
            </ul>
            <p class="mt-3 text-xs text-muted-foreground">
                Администратор проверит запрос и откроет доступ к отметкам.
            </p>
        </section>
    {/if}

    {#if !data.children.length}
        <Empty.Root class="border">
            <Empty.Header>
                <Empty.Media variant="icon"><UserPlusIcon /></Empty.Media>
                <Empty.Title>Пока нет доступа к отметкам</Empty.Title>
                <Empty.Description>
                    Отправьте запрос: укажите кружок и ФИО ребёнка.
                    Администратор сверит данные и откроет доступ. Если
                    администратор прислал ссылку-приглашение — просто откройте
                    её.
                </Empty.Description>
            </Empty.Header>
            <Empty.Content>
                <Button href="/parent/requests">Запросить доступ</Button>
            </Empty.Content>
        </Empty.Root>
    {/if}

    <div class="flex flex-col gap-6">
        {#each data.children as child (child.id)}
            <section class="rounded-xl border">
                <header class="flex items-baseline gap-2 border-b px-4 py-3">
                    <h2 class="text-lg font-semibold">{fullName(child)}</h2>
                    {#if child.grade}
                        <span class="text-sm text-muted-foreground">
                            {child.grade}
                        </span>
                    {/if}
                </header>

                {#if !child.groups.length}
                    <p class="px-4 py-4 text-sm text-muted-foreground">
                        Ребёнок пока не записан ни в один кружок.
                    </p>
                {/if}

                <ul class="divide-y">
                    {#each child.groups as group (group.id)}
                        <li>
                            <a
                                href="/parent/children/{child.id}?group={group.id}"
                                class="flex items-center gap-3 px-4 py-4 hover:bg-muted/50"
                            >
                                <div class="min-w-0 flex-1">
                                    <div
                                        class="flex flex-wrap items-center gap-2"
                                    >
                                        <span class="font-medium">
                                            {group.title}
                                        </span>
                                        {#if group.position}
                                            <Badge variant="secondary">
                                                <TrophyIcon />
                                                {POSITION_LABELS[
                                                    group.position
                                                ]}
                                            </Badge>
                                        {/if}
                                    </div>
                                    {#if group.lastLesson}
                                        {@const lesson = group.lastLesson}
                                        {#if lesson.attended}
                                            <p
                                                class="mt-1 text-2xl font-semibold"
                                            >
                                                {lesson.solved}
                                                <span
                                                    class="text-base font-normal text-muted-foreground"
                                                >
                                                    из {lesson.taskCount} задач
                                                </span>
                                            </p>
                                        {:else}
                                            <p
                                                class="mt-1 text-base text-muted-foreground"
                                            >
                                                Последнее занятие пропущено
                                            </p>
                                        {/if}
                                        <p
                                            class="text-sm text-muted-foreground"
                                        >
                                            {formatDate(
                                                lesson.date,
                                            )}{lesson.topic
                                                ? ` · ${lesson.topic}`
                                                : ""}{lesson.attendance ===
                                            "Late"
                                                ? " · опоздание"
                                                : ""}
                                            {#if lesson.groupAverageSolved !== null}
                                                <br />В среднем по группе — {formatDecimal(
                                                    lesson.groupAverageSolved,
                                                )}
                                            {/if}
                                        </p>
                                    {:else}
                                        <p
                                            class="mt-1 text-sm text-muted-foreground"
                                        >
                                            Опубликованных занятий пока нет
                                        </p>
                                    {/if}
                                </div>
                                <ChevronRightIcon
                                    class="size-5 shrink-0 text-muted-foreground"
                                />
                            </a>
                        </li>
                    {/each}
                </ul>
            </section>
        {/each}
    </div>
</div>
