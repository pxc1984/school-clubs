<script lang="ts">
    import type { GradebookEntryTaskStatus } from "$lib/api/types";
    import { GRADES } from "$lib/grades";
    import { cn } from "$lib/utils";

    let {
        status,
        withLabel = false,
        class: className,
    }: {
        status: GradebookEntryTaskStatus | null;
        withLabel?: boolean;
        class?: string;
    } = $props();

    const grade = $derived(status ? GRADES[status] : null);
</script>

{#if grade}
    <span
        class={cn(
            "inline-flex h-6 min-w-7 items-center justify-center gap-1 rounded-md px-1.5 font-mono text-sm font-semibold",
            grade.className,
            className,
        )}
        title={grade.label}
    >
        {grade.symbol}
        {#if withLabel}
            <span class="font-sans text-xs font-medium">{grade.label}</span>
        {:else}
            <span class="sr-only">{grade.label}</span>
        {/if}
    </span>
{:else}
    <span
        class={cn(
            "inline-flex h-6 min-w-7 items-center justify-center rounded-md border border-dashed px-1.5 text-sm text-muted-foreground",
            className,
        )}
        title="Нет отметки"
    >
        ·
        {#if withLabel}
            <span class="text-xs">Нет отметки</span>
        {:else}
            <span class="sr-only">Нет отметки</span>
        {/if}
    </span>
{/if}
