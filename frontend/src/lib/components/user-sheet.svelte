<script lang="ts">
    import { toast } from "svelte-sonner";
    import BanIcon from "@lucide/svelte/icons/ban";
    import CopyIcon from "@lucide/svelte/icons/copy";
    import KeyRoundIcon from "@lucide/svelte/icons/key-round";
    import LinkIcon from "@lucide/svelte/icons/link";
    import XIcon from "@lucide/svelte/icons/x";
    import {
        getUser,
        linkChild,
        listStudents,
        resetUserPassword,
        setUserBlocked,
        setUserRoles,
        unlinkChild,
        type UserDetailsDto,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import type { StudentDto, UserRole } from "$lib/api/types";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import SearchPicker from "$lib/components/search-picker.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Separator } from "$lib/components/ui/separator";
    import * as Sheet from "$lib/components/ui/sheet";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import { Spinner } from "$lib/components/ui/spinner";
    import { formatDateTime, fullName, ROLE_LABELS } from "$lib/format";
    import { session } from "$lib/session.svelte";

    const ROLES: UserRole[] = ["Parent", "Teacher", "Admin"];

    let {
        userId,
        onClose,
        onChanged,
    }: {
        /** null — шторка закрыта */
        userId: string | null;
        onClose: () => void;
        onChanged: () => Promise<void> | void;
    } = $props();

    let user = $state<UserDetailsDto | null>(null);
    let loadError = $state<string | null>(null);
    let roles = $state<UserRole[]>([]);
    let busy = $state(false);

    $effect(() => {
        const id = userId;
        user = null;
        loadError = null;
        if (!id) return;
        getUser(id)
            .then((loaded) => {
                if (userId !== id) return;
                user = loaded;
                roles = [...loaded.roles];
            })
            .catch((e) => (loadError = errorMessage(e)));
    });

    const isSelf = $derived(user?.id === session.me?.id);
    const rolesChanged = $derived(
        !!user &&
            (roles.length !== user.roles.length ||
                roles.some((role) => !user!.roles.includes(role))),
    );

    /** Применить ответ сервера и обновить список */
    async function updated(next: UserDetailsDto) {
        user = next;
        roles = [...next.roles];
        await onChanged();
    }

    function toggleRole(role: UserRole, checked: boolean) {
        roles = checked
            ? ROLES.filter((r) => r === role || roles.includes(r))
            : roles.filter((r) => r !== role);
    }

    async function saveRoles() {
        if (!user) return;
        busy = true;
        try {
            await updated(await setUserRoles(user.id, roles));
            toast.success("Роли сохранены");
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }

    let blockOpen = $state(false);
    let resetOpen = $state(false);
    let password = $state<string | null>(null);
    let linkOpen = $state(false);
    let unlinking = $state<StudentDto | null>(null);
    let unlinkOpen = $state(false);

    async function addChild(student: { id: string }) {
        if (!user) return;
        try {
            await updated(await linkChild(user.id, student.id));
            linkOpen = false;
        } catch (e) {
            toast.error(errorMessage(e));
        }
    }

    async function copyPassword() {
        if (!password) return;
        try {
            await navigator.clipboard.writeText(password);
            toast.success("Скопировано");
        } catch {
            toast.error("Не удалось скопировать — выделите пароль вручную");
        }
    }
</script>

<Sheet.Root
    open={userId !== null}
    onOpenChange={(open) => {
        if (!open) onClose();
    }}
>
    <Sheet.Content class="w-full overflow-y-auto sm:max-w-md">
        {#if loadError}
            <Sheet.Header>
                <Sheet.Title>Пользователь</Sheet.Title>
            </Sheet.Header>
            <Alert.Root variant="destructive" class="mx-4 w-auto">
                <Alert.Description>{loadError}</Alert.Description>
            </Alert.Root>
        {:else if !user}
            <Sheet.Header>
                <Sheet.Title>Пользователь</Sheet.Title>
            </Sheet.Header>
            <div class="flex flex-col gap-3 px-4">
                <Skeleton class="h-6 w-2/3" />
                <Skeleton class="h-4 w-1/2" />
                <Skeleton class="h-24 w-full" />
            </div>
        {:else}
            <Sheet.Header>
                <Sheet.Title class="flex flex-wrap items-center gap-2">
                    {fullName(user)}
                    {#if user.isBlocked}
                        <Badge variant="destructive">Заблокирован</Badge>
                    {/if}
                </Sheet.Title>
                <Sheet.Description>
                    {user.email}{user.phone ? ` · ${user.phone}` : ""}
                </Sheet.Description>
            </Sheet.Header>

            <div class="flex flex-col gap-6 px-4 pb-6 text-sm">
                <dl class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
                    <dt class="text-muted-foreground">Вход</dt>
                    <dd>
                        {user.loginProviders.length
                            ? user.loginProviders
                                  .map((p) =>
                                      p === "yandex" ? "Яндекс ID" : p,
                                  )
                                  .join(", ")
                            : "по паролю"}
                    </dd>
                    <dt class="text-muted-foreground">Согласие на ПДн</dt>
                    <dd>
                        {user.consentAcceptedAt
                            ? formatDateTime(user.consentAcceptedAt)
                            : "не дано"}
                    </dd>
                </dl>

                <section class="flex flex-col gap-3">
                    <h3 class="font-medium">Роли</h3>
                    {#each ROLES as role (role)}
                        <Field.Field orientation="horizontal">
                            <Checkbox
                                id="role-{role}"
                                checked={roles.includes(role)}
                                disabled={isSelf && role === "Admin"}
                                onCheckedChange={(checked) =>
                                    toggleRole(role, checked)}
                            />
                            <Field.Label for="role-{role}" class="font-normal">
                                {ROLE_LABELS[role]}
                            </Field.Label>
                        </Field.Field>
                    {/each}
                    {#if rolesChanged}
                        <Button
                            size="sm"
                            class="w-fit"
                            disabled={busy}
                            onclick={saveRoles}
                        >
                            {#if busy}<Spinner />{/if}
                            Сохранить роли
                        </Button>
                    {/if}
                </section>

                <Separator />

                <section class="flex flex-col gap-2">
                    <div class="flex items-center justify-between gap-2">
                        <h3 class="font-medium">Дети</h3>
                        <Button
                            variant="outline"
                            size="sm"
                            onclick={() => (linkOpen = true)}
                        >
                            <LinkIcon />
                            Привязать
                        </Button>
                    </div>
                    {#if user.children.length}
                        <ul class="divide-y rounded-lg border">
                            {#each user.children as child (child.id)}
                                <li class="flex items-center gap-2 px-3 py-2">
                                    <span class="min-w-0 flex-1 truncate">
                                        {fullName(child)}
                                    </span>
                                    {#if child.grade}
                                        <span class="text-muted-foreground">
                                            {child.grade}
                                        </span>
                                    {/if}
                                    <Button
                                        variant="ghost"
                                        size="icon-sm"
                                        title="Отвязать"
                                        onclick={() => {
                                            unlinking = child;
                                            unlinkOpen = true;
                                        }}
                                    >
                                        <XIcon />
                                        <span class="sr-only">Отвязать</span>
                                    </Button>
                                </li>
                            {/each}
                        </ul>
                    {:else}
                        <p class="text-muted-foreground">Не привязаны.</p>
                    {/if}
                </section>

                {#if user.groups.length}
                    <section class="flex flex-col gap-2">
                        <h3 class="font-medium">Ведёт группы</h3>
                        <ul class="flex flex-wrap gap-1.5">
                            {#each user.groups as group (group.id)}
                                <li>
                                    <Badge
                                        href="/admin/groups/{group.id}"
                                        variant="outline"
                                    >
                                        {group.title}
                                        {#if group.isArchived}(архив){/if}
                                    </Badge>
                                </li>
                            {/each}
                        </ul>
                    </section>
                {/if}

                <Separator />

                <section class="flex flex-wrap gap-2">
                    <Button
                        variant="outline"
                        size="sm"
                        onclick={() => (resetOpen = true)}
                    >
                        <KeyRoundIcon />
                        Сбросить пароль
                    </Button>
                    {#if !isSelf}
                        <Button
                            variant={user.isBlocked ? "outline" : "destructive"}
                            size="sm"
                            onclick={() => (blockOpen = true)}
                        >
                            <BanIcon />
                            {user.isBlocked
                                ? "Разблокировать"
                                : "Заблокировать"}
                        </Button>
                    {/if}
                </section>
            </div>
        {/if}
    </Sheet.Content>
</Sheet.Root>

{#if user}
    {@const current = user}
    <ConfirmDialog
        bind:open={blockOpen}
        title={current.isBlocked
            ? "Разблокировать пользователя?"
            : "Заблокировать пользователя?"}
        description={current.isBlocked
            ? `${fullName(current)} снова сможет входить.`
            : `${fullName(current)} не сможет войти, текущие сеансы завершатся.`}
        confirmText={current.isBlocked ? "Разблокировать" : "Заблокировать"}
        destructive={!current.isBlocked}
        onConfirm={async () => {
            await updated(await setUserBlocked(current.id, !current.isBlocked));
        }}
    />

    <ConfirmDialog
        bind:open={resetOpen}
        title="Сбросить пароль?"
        description="Будет выдан временный пароль. Он покажется один раз — передайте его пользователю, он сменит его в профиле."
        confirmText="Сбросить"
        onConfirm={async () => {
            password = await resetUserPassword(current.id);
        }}
    />

    <ConfirmDialog
        bind:open={unlinkOpen}
        title="Отвязать ребёнка?"
        description={unlinking
            ? `${fullName(current)} перестанет видеть отметки: ${fullName(unlinking)}.`
            : undefined}
        confirmText="Отвязать"
        destructive
        onConfirm={async () => {
            if (!unlinking) return;
            await updated(await unlinkChild(current.id, unlinking.id));
        }}
    />

    <Dialog.Root bind:open={linkOpen}>
        <Dialog.Content class="sm:max-w-md">
            <Dialog.Header>
                <Dialog.Title>Привязать ребёнка</Dialog.Title>
                <Dialog.Description>
                    Родитель увидит опубликованные отметки ученика. Обычно
                    привязка делается одобрением запроса доступа.
                </Dialog.Description>
            </Dialog.Header>
            <SearchPicker
                search={(q) => listStudents(q)}
                key={(student) => student.id}
                exclude={current.children.map((child) => child.id)}
                onSelect={addChild}
                label="Поиск ученика"
            >
                {#snippet item(student)}
                    <span class="min-w-0 flex-1 truncate">
                        {fullName(student)}
                    </span>
                    <span class="truncate text-muted-foreground">
                        {[
                            student.grade,
                            ...student.groups.map((group) => group.title),
                        ]
                            .filter(Boolean)
                            .join(" · ")}
                    </span>
                {/snippet}
            </SearchPicker>
        </Dialog.Content>
    </Dialog.Root>
{/if}

<Dialog.Root
    open={password !== null}
    onOpenChange={(open) => {
        if (!open) password = null;
    }}
>
    <Dialog.Content class="sm:max-w-sm">
        <Dialog.Header>
            <Dialog.Title>Временный пароль</Dialog.Title>
            <Dialog.Description>
                Больше он нигде не покажется. Передайте его пользователю удобным
                способом.
            </Dialog.Description>
        </Dialog.Header>
        <div class="flex items-center gap-2">
            <code
                class="flex-1 rounded-md bg-muted px-3 py-2 font-mono text-base select-all"
            >
                {password}
            </code>
            <Button variant="outline" size="icon" onclick={copyPassword}>
                <CopyIcon />
                <span class="sr-only">Скопировать</span>
            </Button>
        </div>
        <Dialog.Footer>
            <Button onclick={() => (password = null)}>Готово</Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
