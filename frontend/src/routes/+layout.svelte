<script lang="ts">
    import "../app.css";

    import { ModeWatcher } from "mode-watcher";
    import { navigating } from "$app/state";
    import { Toaster } from "$lib/components/ui/sonner";
    import * as Tooltip from "$lib/components/ui/tooltip";

    let { children } = $props();
</script>

<ModeWatcher />
<Toaster richColors position="top-center" />

{#if navigating.to}
    <div
        class="fixed inset-x-0 top-0 z-50 h-0.5 animate-pulse bg-primary"
        role="progressbar"
        aria-label="Загрузка"
    ></div>
{/if}

<Tooltip.Provider>
    {@render children?.()}
</Tooltip.Provider>
