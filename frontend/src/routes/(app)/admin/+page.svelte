<script lang="ts">
    import { goto, invalidate, invalidateAll } from "$app/navigation";
    import { page } from "$app/state";
    import InboxIcon from "@lucide/svelte/icons/inbox";
    import PageHeader from "$lib/components/page-header.svelte";
    import RequestSheet from "$lib/components/request-sheet.svelte";
    import RequestStatusBadge from "$lib/components/request-status-badge.svelte";
    import * as Empty from "$lib/components/ui/empty";
    import * as ToggleGroup from "$lib/components/ui/toggle-group";
    import { formatDateTime, fullName } from "$lib/format";

    let { data } = $props();

    const FILTERS = [
        { value: "Pending", label: "Ждут решения" },
        { value: "Approved", label: "Одобренные" },
        { value: "Rejected", label: "Отклонённые" },
        { value: "all", label: "Все" },
    ];

    function setParam(name: string, value: string | null) {
        const url = new URL(page.url);
        if (value) url.searchParams.set(name, value);
        else url.searchParams.delete(name);
        void goto(url, { replaceState: true, noScroll: true, keepFocus: true });
    }

    const selected = $derived(
        data.requests.find((request) => request.id === data.selected) ?? null,
    );
</script>

<svelte:head><title>Запросы доступа — Кружки 218</title></svelte:head>

<PageHeader
    title="Запросы доступа"
    description="Родители просят открыть отметки ребёнка. Сверьте ФИО со списком группы и привяжите к ученику."
/>

<ToggleGroup.Root
    type="single"
    variant="outline"
    size="sm"
    class="mb-4"
    value={data.status ?? "all"}
    onValueChange={(value) => {
        if (value) setParam("status", value === "Pending" ? null : value);
    }}
>
    {#each FILTERS as filter (filter.value)}
        <ToggleGroup.Item value={filter.value}>{filter.label}</ToggleGroup.Item>
    {/each}
</ToggleGroup.Root>

{#if !data.requests.length}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Media variant="icon"><InboxIcon /></Empty.Media>
            <Empty.Title>
                {data.status === "Pending" ? "Очередь пуста" : "Запросов нет"}
            </Empty.Title>
            {#if data.status === "Pending"}
                <Empty.Description>
                    Новые запросы родителей появятся здесь.
                </Empty.Description>
            {/if}
        </Empty.Header>
    </Empty.Root>
{:else}
    <ul class="divide-y rounded-xl border">
        {#each data.requests as request (request.id)}
            <li>
                <button
                    type="button"
                    class="flex w-full flex-wrap items-center gap-x-3 gap-y-1 px-4 py-3 text-left hover:bg-muted/50 aria-pressed:bg-muted"
                    aria-pressed={data.selected === request.id}
                    onclick={() => setParam("request", request.id)}
                >
                    <span class="min-w-0 flex-1">
                        <span class="font-medium">
                            {request.childLastName}
                            {request.childFirstName}
                        </span>
                        <span class="text-muted-foreground">
                            · {request.group.title}
                        </span>
                        <span
                            class="block truncate text-sm text-muted-foreground"
                        >
                            от {fullName(request.parent)} ·
                            {formatDateTime(request.created)}
                        </span>
                    </span>
                    {#if request.suggestions.length && request.status === "Pending"}
                        <span class="text-xs text-muted-foreground">
                            есть совпадение
                        </span>
                    {/if}
                    <RequestStatusBadge status={request.status} />
                </button>
            </li>
        {/each}
    </ul>
{/if}

<RequestSheet
    request={selected}
    onClose={() => setParam("request", null)}
    onDecided={async () => {
        setParam("request", null);
        await invalidateAll();
        await invalidate("admin:counters");
    }}
/>
