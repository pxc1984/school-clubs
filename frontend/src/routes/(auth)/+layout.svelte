<script lang="ts">
    let { children } = $props();
</script>

<div
    class="flex min-h-screen flex-col items-center px-4 py-10 sm:justify-center"
>
    <a href="/" class="mb-8 text-lg font-semibold tracking-tight">Кружки 218</a>
    <main class="w-full max-w-sm">
        {@render children()}
    </main>
</div>
