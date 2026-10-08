<script lang="ts">
    import { onMount } from "svelte";
    import {
        createTodoItem,
        deleteTodoItem,
        listTodoItems,
        updateTodoItem,
        type TodoItem,
    } from "$lib/api/todo-items";

    let items = $state<TodoItem[]>([]);
    let title = $state("");
    let error = $state<string | null>(null);

    async function load() {
        try {
            items = await listTodoItems();
            error = null;
        } catch {
            error = "Backend is unreachable";
        }
    }

    async function add(event: SubmitEvent) {
        event.preventDefault();
        if (!title.trim()) return;
        await createTodoItem(title);
        title = "";
        await load();
    }

    async function toggle(item: TodoItem) {
        await updateTodoItem({ ...item, isDone: !item.isDone });
        await load();
    }

    async function remove(item: TodoItem) {
        await deleteTodoItem(item.id);
        await load();
    }

    onMount(load);
</script>

<main class="mx-auto max-w-xl p-6">
    <h1 class="mb-6 text-2xl font-semibold">Todo items</h1>

    <form class="mb-4 flex gap-2" onsubmit={add}>
        <input
            class="flex-1 rounded-md border border-neutral-300 bg-transparent px-3 py-2 dark:border-neutral-700"
            placeholder="What needs to be done?"
            bind:value={title}
        />
        <button
            class="rounded-md bg-neutral-900 px-4 py-2 text-white dark:bg-neutral-100 dark:text-neutral-900"
        >
            Add
        </button>
    </form>

    {#if error}
        <p class="text-red-600">{error}</p>
    {/if}

    <ul class="divide-y divide-neutral-200 dark:divide-neutral-800">
        {#each items as item (item.id)}
            <li class="flex items-center gap-3 py-2">
                <input
                    type="checkbox"
                    checked={item.isDone}
                    onchange={() => toggle(item)}
                />
                <span class="flex-1" class:line-through={item.isDone}>
                    {item.title}
                </span>
                <button
                    class="text-sm text-neutral-500 hover:text-red-600"
                    onclick={() => remove(item)}
                >
                    Delete
                </button>
            </li>
        {/each}
    </ul>
</main>
