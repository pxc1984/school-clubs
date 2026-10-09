<script lang="ts" generics="T">
    import type { Snippet } from "svelte";
    import SearchIcon from "@lucide/svelte/icons/search";
    import { errorMessage } from "$lib/api/client";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { cn } from "$lib/utils";

    /** Поле поиска со списком результатов: выбор ученика, учителя и т.п. */
    let {
        search,
        key,
        item,
        onSelect,
        selected = null,
        exclude = [],
        placeholder = "Фамилия или имя",
        label = "Поиск",
        initialQuery = "",
    }: {
        search: (query: string) => Promise<T[]>;
        key: (item: T) => string;
        item: Snippet<[T]>;
        onSelect: (item: T) => void;
        /** Подсветить выбранный */
        selected?: string | null;
        /** Не показывать (уже добавленные) */
        exclude?: string[];
        placeholder?: string;
        label?: string;
        initialQuery?: string;
    } = $props();

    // svelte-ignore state_referenced_locally
    let query = $state(initialQuery);
    let results = $state<T[]>([]);
    let loading = $state(false);
    let error = $state<string | null>(null);
    let searched = $state(false);

    // запрос с задержкой; ответ на устаревший запрос отбрасываем
    let requestId = 0;
    $effect(() => {
        const current = query.trim();
        const id = ++requestId;
        const timer = setTimeout(async () => {
            loading = true;
            error = null;
            try {
                const found = await search(current);
                if (id === requestId) results = found;
            } catch (e) {
                if (id === requestId) error = errorMessage(e);
            } finally {
                if (id === requestId) {
                    loading = false;
                    searched = true;
                }
            }
        }, 300);
        return () => clearTimeout(timer);
    });

    const visible = $derived(
        results.filter((result) => !exclude.includes(key(result))),
    );
</script>

<div class="flex flex-col gap-2">
    <div class="relative">
        <SearchIcon
            class="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
        />
        <Input
            type="search"
            class="pl-8"
            aria-label={label}
            {placeholder}
            bind:value={query}
        />
        {#if loading}
            <Spinner
                class="absolute top-1/2 right-2.5 size-4 -translate-y-1/2"
            />
        {/if}
    </div>
    {#if error}
        <p class="text-sm text-destructive">{error}</p>
    {:else if searched && !visible.length}
        <p class="px-1 py-2 text-sm text-muted-foreground">Никого не нашли</p>
    {:else if visible.length}
        <ul class="max-h-64 divide-y overflow-y-auto rounded-lg border">
            {#each visible as result (key(result))}
                <li>
                    <button
                        type="button"
                        class={cn(
                            "flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-muted",
                            selected === key(result) &&
                                "bg-secondary hover:bg-secondary",
                        )}
                        aria-pressed={selected === key(result)}
                        onclick={() => onSelect(result)}
                    >
                        {@render item(result)}
                    </button>
                </li>
            {/each}
        </ul>
    {/if}
</div>
