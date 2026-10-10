<script lang="ts">
    import { toast } from "svelte-sonner";
    import { goto, invalidateAll } from "$app/navigation";
    import PencilIcon from "@lucide/svelte/icons/pencil";
    import Trash2Icon from "@lucide/svelte/icons/trash-2";
    import UserPlusIcon from "@lucide/svelte/icons/user-plus";
    import XIcon from "@lucide/svelte/icons/x";
    import {
        addGroupTeacher,
        deleteGroup,
        listUsers,
        removeAdminGroupStudent,
        removeGroupTeacher,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import type { StudentDto, UserRefDto } from "$lib/api/types";
    import AddStudentsDialog from "$lib/components/add-students-dialog.svelte";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import GroupDialog from "$lib/components/group-dialog.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import SearchPicker from "$lib/components/search-picker.svelte";
    import StudentSheet from "$lib/components/student-sheet.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import { fullName } from "$lib/format";

    let { data } = $props();

    const group = $derived(data.group);

    let editOpen = $state(false);
    let deleteOpen = $state(false);
    let teacherOpen = $state(false);
    let studentsOpen = $state(false);

    let removingTeacher = $state<UserRefDto | null>(null);
    let removeTeacherOpen = $state(false);
    let removingStudent = $state<StudentDto | null>(null);
    let removeStudentOpen = $state(false);
    /** Карточка ученика — прямо здесь, чтобы не терять группу (ссылка для родителя, правка) */
    let openedStudent = $state<string | null>(null);

    async function addTeacher(teacher: { id: string }) {
        try {
            await addGroupTeacher(group.id, teacher.id);
            await invalidateAll();
            teacherOpen = false;
        } catch (e) {
            toast.error(errorMessage(e));
        }
    }
</script>

<svelte:head><title>{group.title} — Кружки 218</title></svelte:head>

<PageHeader
    title={group.title}
    description={group.description}
    back={{ href: "/admin/groups", label: "Группы" }}
>
    {#snippet badges()}
        {#if group.isArchived}
            <Badge variant="secondary">В архиве</Badge>
        {/if}
    {/snippet}
    {#snippet actions()}
        <Button variant="outline" onclick={() => (editOpen = true)}>
            <PencilIcon />
            Изменить
        </Button>
        <Button variant="outline" onclick={() => (deleteOpen = true)}>
            <Trash2Icon />
            Удалить
        </Button>
    {/snippet}
</PageHeader>

<div class="grid gap-8 lg:grid-cols-[1fr_2fr]">
    <section class="flex flex-col gap-3">
        <div class="flex items-center justify-between gap-2">
            <h2 class="font-medium">Учителя</h2>
            <Button
                variant="outline"
                size="sm"
                onclick={() => (teacherOpen = true)}
            >
                <UserPlusIcon />
                Назначить
            </Button>
        </div>
        {#if group.teachers.length}
            <ul class="divide-y rounded-lg border">
                {#each group.teachers as teacher (teacher.id)}
                    <li class="flex items-center gap-2 px-3 py-2 text-sm">
                        <a
                            href="/admin/users?q={encodeURIComponent(
                                teacher.lastName,
                            )}"
                            class="min-w-0 flex-1 truncate hover:underline"
                        >
                            {fullName(teacher)}
                        </a>
                        <Button
                            variant="ghost"
                            size="icon-sm"
                            title="Снять с группы"
                            onclick={() => {
                                removingTeacher = teacher;
                                removeTeacherOpen = true;
                            }}
                        >
                            <XIcon />
                            <span class="sr-only">Снять с группы</span>
                        </Button>
                    </li>
                {/each}
            </ul>
        {:else}
            <p
                class="rounded-lg border border-dashed px-3 py-4 text-sm text-muted-foreground"
            >
                Учитель не назначен — журналы вести некому.
            </p>
        {/if}
    </section>

    <section class="flex flex-col gap-3">
        <div class="flex items-center justify-between gap-2">
            <h2 class="font-medium">
                Ученики
                <span class="text-muted-foreground">
                    {group.students.length}
                </span>
            </h2>
            <Button
                variant="outline"
                size="sm"
                onclick={() => (studentsOpen = true)}
            >
                <UserPlusIcon />
                Добавить
            </Button>
        </div>
        {#if group.students.length}
            <ul class="divide-y rounded-lg border">
                {#each group.students as student (student.id)}
                    <li class="flex items-center gap-2 px-3 py-2 text-sm">
                        <button
                            type="button"
                            class="min-w-0 flex-1 truncate text-left hover:underline"
                            onclick={() => (openedStudent = student.id)}
                        >
                            {fullName(student)}
                        </button>
                        {#if student.grade}
                            <span class="text-muted-foreground">
                                {student.grade}
                            </span>
                        {/if}
                        <Button
                            variant="ghost"
                            size="icon-sm"
                            title="Убрать из группы"
                            onclick={() => {
                                removingStudent = student;
                                removeStudentOpen = true;
                            }}
                        >
                            <XIcon />
                            <span class="sr-only">Убрать из группы</span>
                        </Button>
                    </li>
                {/each}
            </ul>
        {:else}
            <p
                class="rounded-lg border border-dashed px-3 py-4 text-sm text-muted-foreground"
            >
                В группе нет учеников.
            </p>
        {/if}
    </section>
</div>

<GroupDialog bind:open={editOpen} {group} onSaved={() => invalidateAll()} />

<Dialog.Root bind:open={teacherOpen}>
    <Dialog.Content class="sm:max-w-md">
        <Dialog.Header>
            <Dialog.Title>Назначить учителя</Dialog.Title>
            <Dialog.Description>
                Среди пользователей с ролью «Учитель». Нового учителя заведите в
                разделе «Пользователи».
            </Dialog.Description>
        </Dialog.Header>
        <SearchPicker
            search={(q) => listUsers(q, "Teacher")}
            key={(user) => user.id}
            exclude={group.teachers.map((teacher) => teacher.id)}
            onSelect={addTeacher}
            placeholder="Фамилия или email"
            label="Поиск учителя"
        >
            {#snippet item(user)}
                <span class="min-w-0 flex-1 truncate">{fullName(user)}</span>
                <span class="truncate text-muted-foreground">{user.email}</span>
            {/snippet}
        </SearchPicker>
    </Dialog.Content>
</Dialog.Root>

<AddStudentsDialog
    bind:open={studentsOpen}
    groupId={group.id}
    existingIds={group.students.map((student) => student.id)}
    onAdded={() => invalidateAll()}
/>

<ConfirmDialog
    bind:open={removeTeacherOpen}
    title="Снять учителя с группы?"
    description={removingTeacher
        ? `${fullName(removingTeacher)} больше не увидит журналы группы «${group.title}».`
        : undefined}
    confirmText="Снять"
    onConfirm={async () => {
        if (!removingTeacher) return;
        await removeGroupTeacher(group.id, removingTeacher.id);
        await invalidateAll();
    }}
/>

<ConfirmDialog
    bind:open={removeStudentOpen}
    title="Убрать ученика из группы?"
    description={removingStudent
        ? `${fullName(removingStudent)} пропадёт из новых журналов. Уже выставленные отметки сохранятся.`
        : undefined}
    confirmText="Убрать"
    onConfirm={async () => {
        if (!removingStudent) return;
        await removeAdminGroupStudent(group.id, removingStudent.id);
        await invalidateAll();
    }}
/>

<ConfirmDialog
    bind:open={deleteOpen}
    title="Удалить группу?"
    description="Удалить можно только группу без занятий. Группу с историей лучше отправить в архив."
    confirmText="Удалить"
    destructive
    onConfirm={async () => {
        await deleteGroup(group.id);
        await goto("/admin/groups");
        toast.success("Группа удалена");
    }}
/>

<StudentSheet
    studentId={openedStudent}
    onClose={() => (openedStudent = null)}
    onChanged={() => invalidateAll()}
    onRemoved={async () => {
        openedStudent = null;
        await invalidateAll();
    }}
/>
