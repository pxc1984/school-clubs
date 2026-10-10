<script lang="ts">
    import { toast } from "svelte-sonner";
    import { goto, invalidate, invalidateAll } from "$app/navigation";
    import { page } from "$app/state";
    import TriangleAlertIcon from "@lucide/svelte/icons/triangle-alert";
    import UserPlusIcon from "@lucide/svelte/icons/user-plus";
    import { createStudent, retryErasure } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import PageHeader from "$lib/components/page-header.svelte";
    import StudentDialog, {
        type StudentForm,
    } from "$lib/components/student-dialog.svelte";
    import StudentSheet from "$lib/components/student-sheet.svelte";
    import UrlSearch from "$lib/components/url-search.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Empty from "$lib/components/ui/empty";
    import * as Table from "$lib/components/ui/table";
    import { formatDate, formatDateTime, fullName } from "$lib/format";

    let { data } = $props();

    function select(id: string | null) {
        const url = new URL(page.url);
        if (id) url.searchParams.set("student", id);
        else url.searchParams.delete("student");
        void goto(url, { replaceState: true, noScroll: true, keepFocus: true });
    }

    let createOpen = $state(false);

    async function create(form: StudentForm) {
        const student = await createStudent({ ...form, groupId: null });
        await invalidateAll();
        select(student.id);
    }

    let retrying = $state<string | null>(null);

    async function retry(id: string) {
        retrying = id;
        try {
            await retryErasure(id);
            await invalidateAll();
            await invalidate("admin:counters");
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            retrying = null;
        }
    }
</script>

<svelte:head><title>Ученики — Кружки 218</title></svelte:head>

<PageHeader title="Ученики">
    {#snippet actions()}
        <Button onclick={() => (createOpen = true)}>
            <UserPlusIcon />
            Новый ученик
        </Button>
    {/snippet}
</PageHeader>

{#if data.erasures.length}
    <Alert.Root class="mb-6">
        <TriangleAlertIcon />
        <Alert.Title>Удаление не завершено</Alert.Title>
        <Alert.Description>
            <p>
                Данные удалены из базы, но не все файлы журналов удалось
                очистить. Повторите попытку.
            </p>
            <ul class="mt-2 flex flex-col gap-1">
                {#each data.erasures as erasure (erasure.id)}
                    <li class="flex flex-wrap items-center gap-2">
                        <span>
                            Заявление от {formatDate(
                                erasure.requestReceivedAt,
                            )}, удалено {formatDateTime(erasure.erasedAt)}
                        </span>
                        <Button
                            variant="outline"
                            size="sm"
                            disabled={retrying === erasure.id}
                            onclick={() => retry(erasure.id)}
                        >
                            Повторить
                        </Button>
                    </li>
                {/each}
            </ul>
        </Alert.Description>
    </Alert.Root>
{/if}

<div class="mb-4">
    <UrlSearch
        value={data.q}
        placeholder="Фамилия или имя"
        label="Поиск учеников"
    />
</div>

{#if !data.students.length}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Title>Никого не нашли</Empty.Title>
            <Empty.Description>Попробуйте фамилию или имя.</Empty.Description>
        </Empty.Header>
    </Empty.Root>
{:else}
    <div class="relative overflow-x-auto rounded-lg border">
        <Table.Root>
            <Table.Header>
                <Table.Row>
                    <Table.Head>ФИО</Table.Head>
                    <Table.Head>Класс</Table.Head>
                    <Table.Head>Группы</Table.Head>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {#each data.students as student (student.id)}
                    <Table.Row
                        class="cursor-pointer"
                        data-state={data.selected === student.id
                            ? "selected"
                            : undefined}
                        onclick={() => select(student.id)}
                    >
                        <Table.Cell>
                            <button
                                type="button"
                                class="text-left font-medium hover:underline"
                                onclick={(event) => {
                                    event.stopPropagation();
                                    select(student.id);
                                }}
                            >
                                {fullName(student)}
                            </button>
                        </Table.Cell>
                        <Table.Cell class="text-muted-foreground">
                            {student.grade ?? "—"}
                        </Table.Cell>
                        <Table.Cell>
                            <div class="flex flex-wrap gap-1">
                                {#each student.groups as group (group.id)}
                                    <Badge
                                        variant={group.isArchived
                                            ? "outline"
                                            : "secondary"}
                                    >
                                        {group.title}
                                    </Badge>
                                {/each}
                            </div>
                        </Table.Cell>
                    </Table.Row>
                {/each}
            </Table.Body>
        </Table.Root>
    </div>
{/if}

<StudentSheet
    studentId={data.selected}
    onClose={() => select(null)}
    onChanged={() => invalidateAll()}
    onRemoved={async (targetId) => {
        select(targetId ?? null);
        await invalidateAll();
    }}
/>

<StudentDialog
    bind:open={createOpen}
    description="Обычно учеников добавляют сразу в группу — на странице группы."
    onSubmit={create}
/>
