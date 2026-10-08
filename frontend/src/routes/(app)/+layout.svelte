<script lang="ts">
    import AppHeader from "$lib/components/app-header.svelte";

    let { children } = $props();
</script>

<AppHeader />

<main class="mx-auto w-full max-w-6xl px-4 py-6 sm:py-8">
    {@render children()}
</main>
