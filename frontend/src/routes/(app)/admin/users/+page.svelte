<script lang="ts">
    import { toast } from "svelte-sonner";
    import { goto, invalidateAll } from "$app/navigation";
    import { page } from "$app/state";
    import UserPlusIcon from "@lucide/svelte/icons/user-plus";
    import { inviteTeacher } from "$lib/api/admin";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import type { UserRole } from "$lib/api/types";
    import PageHeader from "$lib/components/page-header.svelte";
    import UrlSearch from "$lib/components/url-search.svelte";
    import UserSheet from "$lib/components/user-sheet.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Empty from "$lib/components/ui/empty";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import { Spinner } from "$lib/components/ui/spinner";
    import * as Table from "$lib/components/ui/table";
    import { fullName, ROLE_LABELS } from "$lib/format";

    let { data } = $props();

    function setParam(name: string, value: string | null) {
        const url = new URL(page.url);
        if (value) url.searchParams.set(name, value);
        else url.searchParams.delete(name);
        void goto(url, { replaceState: true, noScroll: true, keepFocus: true });
    }

    // ---------- приглашение учителя ----------

    let inviteOpen = $state(false);
    let invite = $state({
        email: "",
        lastName: "",
        firstName: "",
        middleName: "",
    });
    let inviteErrors = $state<Record<string, string>>({});
    let inviteBusy = $state(false);

    function openInvite() {
        invite = { email: "", lastName: "", firstName: "", middleName: "" };
        inviteErrors = {};
        inviteOpen = true;
    }

    async function submitInvite(event: SubmitEvent) {
        event.preventDefault();
        inviteErrors = {};
        if (!invite.email.trim()) inviteErrors.email = "Укажите email";
        if (!invite.lastName.trim()) inviteErrors.lastName = "Укажите фамилию";
        if (!invite.firstName.trim()) inviteErrors.firstName = "Укажите имя";
        if (Object.keys(inviteErrors).length) return;

        inviteBusy = true;
        try {
            const user = await inviteTeacher({
                email: invite.email.trim(),
                lastName: invite.lastName.trim(),
                firstName: invite.firstName.trim(),
                middleName: invite.middleName.trim() || null,
            });
            inviteOpen = false;
            toast.success("Учитель заведён", {
                description:
                    "Он войдёт через Яндекс ID с этим email или по временному паролю — его можно выдать в карточке.",
            });
            setParam("user", user.id);
            await invalidateAll();
        } catch (e) {
            inviteErrors = fieldErrors(e);
            if (!Object.keys(inviteErrors).length) toast.error(errorMessage(e));
        } finally {
            inviteBusy = false;
        }
    }
</script>

<svelte:head><title>Пользователи — Кружки 218</title></svelte:head>

<PageHeader title="Пользователи">
    {#snippet actions()}
        <Button onclick={openInvite}>
            <UserPlusIcon />
            Завести учителя
        </Button>
    {/snippet}
</PageHeader>

<div class="mb-4 flex flex-wrap items-center gap-2">
    <UrlSearch
        value={data.q}
        placeholder="Фамилия, имя или email"
        label="Поиск пользователей"
    />
    <NativeSelect.Root
        aria-label="Роль"
        value={data.role ?? ""}
        onchange={(event) =>
            setParam("role", (event.currentTarget as HTMLSelectElement).value)}
    >
        <NativeSelect.Option value="">Все роли</NativeSelect.Option>
        {#each Object.entries(ROLE_LABELS) as [role, label] (role)}
            <NativeSelect.Option value={role}>{label}</NativeSelect.Option>
        {/each}
    </NativeSelect.Root>
</div>

{#if !data.users.length}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Title>Никого не нашли</Empty.Title>
            <Empty.Description>
                Попробуйте фамилию или часть email.
            </Empty.Description>
        </Empty.Header>
    </Empty.Root>
{:else}
    <div class="relative overflow-x-auto rounded-lg border">
        <Table.Root>
            <Table.Header>
                <Table.Row>
                    <Table.Head>ФИО</Table.Head>
                    <Table.Head>Email</Table.Head>
                    <Table.Head>Роли</Table.Head>
                </Table.Row>
            </Table.Header>
            <Table.Body>
                {#each data.users as user (user.id)}
                    <Table.Row
                        class="cursor-pointer"
                        data-state={data.selected === user.id
                            ? "selected"
                            : undefined}
                        onclick={() => setParam("user", user.id)}
                    >
                        <Table.Cell>
                            <button
                                type="button"
                                class="text-left font-medium hover:underline"
                                onclick={(event) => {
                                    event.stopPropagation();
                                    setParam("user", user.id);
                                }}
                            >
                                {fullName(user)}
                            </button>
                            {#if user.isBlocked}
                                <Badge variant="destructive" class="ml-1">
                                    заблокирован
                                </Badge>
                            {/if}
                        </Table.Cell>
                        <Table.Cell class="text-muted-foreground">
                            {user.email}
                        </Table.Cell>
                        <Table.Cell>
                            <div class="flex flex-wrap gap-1">
                                {#each user.roles as role (role)}
                                    <Badge variant="secondary">
                                        {ROLE_LABELS[role as UserRole]}
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

<UserSheet
    userId={data.selected}
    onClose={() => setParam("user", null)}
    onChanged={() => invalidateAll()}
/>

<Dialog.Root bind:open={inviteOpen}>
    <Dialog.Content class="sm:max-w-md">
        <Dialog.Header>
            <Dialog.Title>Завести учителя</Dialog.Title>
            <Dialog.Description>
                Если пользователь с таким email уже есть, ему добавится роль
                «Учитель».
            </Dialog.Description>
        </Dialog.Header>
        <form id="invite-form" onsubmit={submitInvite} novalidate>
            <Field.Group>
                <Field.Field data-invalid={!!inviteErrors.email}>
                    <Field.Label for="invite-email">Email</Field.Label>
                    <Input
                        id="invite-email"
                        type="email"
                        autocomplete="off"
                        bind:value={invite.email}
                        aria-invalid={!!inviteErrors.email}
                    />
                    <Field.Error>{inviteErrors.email}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!inviteErrors.lastName}>
                    <Field.Label for="invite-last">Фамилия</Field.Label>
                    <Input
                        id="invite-last"
                        autocomplete="off"
                        bind:value={invite.lastName}
                        aria-invalid={!!inviteErrors.lastName}
                    />
                    <Field.Error>{inviteErrors.lastName}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!inviteErrors.firstName}>
                    <Field.Label for="invite-first">Имя</Field.Label>
                    <Input
                        id="invite-first"
                        autocomplete="off"
                        bind:value={invite.firstName}
                        aria-invalid={!!inviteErrors.firstName}
                    />
                    <Field.Error>{inviteErrors.firstName}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!inviteErrors.middleName}>
                    <Field.Label for="invite-middle">Отчество</Field.Label>
                    <Input
                        id="invite-middle"
                        autocomplete="off"
                        placeholder="Необязательно"
                        bind:value={invite.middleName}
                    />
                    <Field.Error>{inviteErrors.middleName}</Field.Error>
                </Field.Field>
            </Field.Group>
        </form>
        <Dialog.Footer>
            <Button variant="outline" onclick={() => (inviteOpen = false)}>
                Отмена
            </Button>
            <Button type="submit" form="invite-form" disabled={inviteBusy}>
                {#if inviteBusy}<Spinner />{/if}
                Завести
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
