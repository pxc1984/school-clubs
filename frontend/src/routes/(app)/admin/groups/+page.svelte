<script lang="ts">
    import { goto } from "$app/navigation";
    import ChevronRightIcon from "@lucide/svelte/icons/chevron-right";
    import PlusIcon from "@lucide/svelte/icons/plus";
    import GroupDialog from "$lib/components/group-dialog.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import * as Empty from "$lib/components/ui/empty";
    import * as Field from "$lib/components/ui/field";
    import { countOf, shortName } from "$lib/format";

    let { data } = $props();

    let createOpen = $state(false);

    function toggleArchived(checked: boolean) {
        void goto(checked ? "?archived=1" : "?", {
            replaceState: true,
            noScroll: true,
            keepFocus: true,
        });
    }
</script>

<svelte:head><title>Группы — Кружки 218</title></svelte:head>

<PageHeader title="Группы">
    {#snippet actions()}
        <Button onclick={() => (createOpen = true)}>
            <PlusIcon />
            Новая группа
        </Button>
    {/snippet}
</PageHeader>

<Field.Field orientation="horizontal" class="mb-4 w-fit">
    <Checkbox
        id="show-archived"
        checked={data.archived}
        onCheckedChange={toggleArchived}
    />
    <Field.Label for="show-archived" class="font-normal">
        Показывать архивные
    </Field.Label>
</Field.Field>

{#if !data.groups.length}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Title>Групп нет</Empty.Title>
            <Empty.Description>
                Создайте группу, назначьте учителя и добавьте учеников.
            </Empty.Description>
        </Empty.Header>
    </Empty.Root>
{:else}
    <ul class="divide-y rounded-xl border">
        {#each data.groups as group (group.id)}
            <li>
                <a
                    href="/admin/groups/{group.id}"
                    class="flex items-center gap-3 px-4 py-3 hover:bg-muted/50"
                >
                    <div class="min-w-0 flex-1">
                        <div class="flex flex-wrap items-center gap-2">
                            <span class="font-medium">{group.title}</span>
                            {#if group.isArchived}
                                <Badge variant="secondary">В архиве</Badge>
                            {/if}
                        </div>
                        <p class="mt-0.5 text-sm text-muted-foreground">
                            {countOf(group.studentCount, [
                                "ученик",
                                "ученика",
                                "учеников",
                            ])} ·
                            {#if group.teachers.length}
                                {group.teachers
                                    .map((teacher) => shortName(teacher))
                                    .join(", ")}
                            {:else}
                                <span
                                    class="text-amber-700 dark:text-amber-400"
                                >
                                    учитель не назначен
                                </span>
                            {/if}
                        </p>
                    </div>
                    <ChevronRightIcon
                        class="size-5 shrink-0 text-muted-foreground"
                    />
                </a>
            </li>
        {/each}
    </ul>
{/if}

<GroupDialog
    bind:open={createOpen}
    onSaved={(group) => goto(`/admin/groups/${group.id}`)}
/>
