<script lang="ts">
    import SearchIcon from "@lucide/svelte/icons/search";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { Input } from "$lib/components/ui/input";

    /** Поле поиска, которое пишет запрос в параметр адреса (?q=) */
    let {
        value,
        param = "q",
        placeholder = "Поиск",
        label = "Поиск",
    }: {
        value: string;
        param?: string;
        placeholder?: string;
        label?: string;
    } = $props();

    // svelte-ignore state_referenced_locally
    let text = $state(value);
    let timer: ReturnType<typeof setTimeout> | undefined;

    function onInput() {
        clearTimeout(timer);
        timer = setTimeout(() => {
            const url = new URL(page.url);
            const query = text.trim();
            if (query) url.searchParams.set(param, query);
            else url.searchParams.delete(param);
            void goto(url, {
                replaceState: true,
                noScroll: true,
                keepFocus: true,
            });
        }, 300);
    }
</script>

<div class="relative w-full max-w-sm">
    <SearchIcon
        class="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
    />
    <Input
        type="search"
        class="pl-8"
        aria-label={label}
        {placeholder}
        bind:value={text}
        oninput={onInput}
    />
</div>
