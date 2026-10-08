<script lang="ts">
    import type { Snippet } from "svelte";
    import ChevronLeftIcon from "@lucide/svelte/icons/chevron-left";

    let {
        title,
        description,
        back,
        badges,
        actions,
    }: {
        title: string;
        description?: string | null;
        back?: { href: string; label: string };
        badges?: Snippet;
        actions?: Snippet;
    } = $props();
</script>

<div class="mb-6 flex flex-col gap-3">
    {#if back}
        <a
            href={back.href}
            class="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
            <ChevronLeftIcon class="size-4" />
            {back.label}
        </a>
    {/if}
    <div class="flex flex-wrap items-start justify-between gap-3">
        <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
                <h1 class="text-2xl font-semibold tracking-tight">{title}</h1>
                {@render badges?.()}
            </div>
            {#if description}
                <p class="mt-1 text-sm text-muted-foreground">{description}</p>
            {/if}
        </div>
        {#if actions}
            <div class="flex flex-wrap items-center gap-2">
                {@render actions()}
            </div>
        {/if}
    </div>
</div>
