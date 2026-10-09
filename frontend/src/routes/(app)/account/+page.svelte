<script lang="ts">
    import { toast } from "svelte-sonner";
    import KeyRoundIcon from "@lucide/svelte/icons/key-round";
    import LinkIcon from "@lucide/svelte/icons/link";
    import { onMount } from "svelte";
    import { invalidateAll, replaceState } from "$app/navigation";
    import { page } from "$app/state";
    import {
        AVATAR_ACCEPT,
        AVATAR_MAX_BYTES,
        avatarUrl,
        changePassword,
        changeUsername,
        deleteAvatar,
        linkProviderUrl,
        unlinkProvider,
        updateAccount,
        uploadAvatar,
    } from "$lib/api/account";
    import { errorMessage, fieldErrors, statusOf } from "$lib/api/client";
    import type { Gender } from "$lib/api/types";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import * as Avatar from "$lib/components/ui/avatar";
    import PageHeader from "$lib/components/page-header.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import { Separator } from "$lib/components/ui/separator";
    import { Spinner } from "$lib/components/ui/spinner";
    import { initials } from "$lib/format";
    import { session } from "$lib/session.svelte";

    let { data } = $props();

    const account = $derived(data.account);
    const hasPassword = $derived(
        account?.loginProviders.includes("password") ?? false,
    );

    // форма профиля инициализируется из данных и дальше живёт сама
    let profile = $state({
        lastName: "",
        firstName: "",
        middleName: "",
        phone: "",
        gender: "" as Gender | "",
    });
    $effect.pre(() => {
        if (!account) return;
        profile = {
            lastName: account.lastName,
            firstName: account.firstName,
            middleName: account.middleName ?? "",
            phone: account.phone ?? "",
            gender: account.gender ?? "",
        };
    });
    let profileErrors = $state<Record<string, string>>({});
    let savingProfile = $state(false);

    async function saveProfile(event: SubmitEvent) {
        event.preventDefault();
        savingProfile = true;
        profileErrors = {};
        try {
            await updateAccount({
                lastName: profile.lastName.trim(),
                firstName: profile.firstName.trim(),
                middleName: profile.middleName.trim() || null,
                phone: profile.phone.trim() || null,
                gender: profile.gender || null,
            });
            await Promise.all([invalidateAll(), session.load(true)]);
            toast.success("Профиль сохранён");
        } catch (e) {
            profileErrors = fieldErrors(e);
            if (!Object.keys(profileErrors).length)
                toast.error(errorMessage(e));
        } finally {
            savingProfile = false;
        }
    }

    const LINK_ERRORS: Record<string, string> = {
        taken: "Этот аккаунт уже привязан к другому пользователю.",
        provider: "У вас уже привязан другой аккаунт этого сервиса.",
        session: "Сессия изменилась во время привязки. Попробуйте ещё раз.",
    };

    // результат привязки провайдера приходит параметрами после редиректа с backend
    onMount(() => {
        const params = page.url.searchParams;
        const linked = params.get("linked");
        const linkError = params.get("linkError");
        if (!linked && !linkError) return;
        if (linked) {
            const name =
                data.providers.find((p) => p.name === linked)?.displayName ??
                linked;
            toast.success(`${name} привязан`);
        } else if (linkError) {
            toast.error(
                LINK_ERRORS[linkError] ??
                    "Не удалось привязать. Попробуйте ещё раз.",
            );
        }
        const url = new URL(page.url);
        url.searchParams.delete("linked");
        url.searchParams.delete("linkError");
        replaceState(url, {});
    });

    let avatarInput = $state<HTMLInputElement | null>(null);
    let savingAvatar = $state(false);
    let deleteAvatarOpen = $state(false);

    async function onAvatarSelected(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        const file = input.files?.[0];
        input.value = "";
        if (!file) return;
        if (file.size > AVATAR_MAX_BYTES) {
            toast.error("Файл больше 2 МБ");
            return;
        }
        savingAvatar = true;
        try {
            await uploadAvatar(file);
            await Promise.all([invalidateAll(), session.load(true)]);
            toast.success("Аватарка обновлена");
        } catch (e) {
            toast.error(fieldErrors(e).file ?? errorMessage(e));
        } finally {
            savingAvatar = false;
        }
    }

    let username = $state("");
    $effect.pre(() => {
        if (account) username = account.username ?? "";
    });
    let usernameError = $state("");
    let savingUsername = $state(false);

    async function saveUsername(event: SubmitEvent) {
        event.preventDefault();
        savingUsername = true;
        usernameError = "";
        try {
            const updated = await changeUsername(username.trim() || null);
            username = updated.username ?? "";
            await invalidateAll();
            toast.success(updated.username ? "Ник сохранён" : "Ник удалён");
        } catch (e) {
            usernameError =
                statusOf(e) === 409
                    ? "Этот ник уже занят"
                    : (fieldErrors(e).username ?? "");
            if (!usernameError) toast.error(errorMessage(e));
        } finally {
            savingUsername = false;
        }
    }

    let password = $state({ current: "", next: "", repeat: "" });
    let passwordErrors = $state<Record<string, string>>({});
    let savingPassword = $state(false);

    async function savePassword(event: SubmitEvent) {
        event.preventDefault();
        passwordErrors = {};
        if (password.next.length < 8) {
            passwordErrors = { newPassword: "Не короче 8 символов" };
            return;
        }
        if (password.next !== password.repeat) {
            passwordErrors = { repeat: "Пароли не совпадают" };
            return;
        }
        savingPassword = true;
        try {
            await changePassword({
                currentPassword: hasPassword ? password.current : null,
                newPassword: password.next,
            });
            password = { current: "", next: "", repeat: "" };
            await invalidateAll();
            toast.success(hasPassword ? "Пароль изменён" : "Пароль задан");
        } catch (e) {
            passwordErrors = fieldErrors(e);
            if (!Object.keys(passwordErrors).length)
                toast.error(errorMessage(e));
        } finally {
            savingPassword = false;
        }
    }

    let unlinking = $state<string | null>(null);
    let unlinkOpen = $state(false);
</script>

<svelte:head><title>Профиль — Кружки 218</title></svelte:head>

<div class="mx-auto max-w-2xl">
    <PageHeader title="Профиль" />

    {#if !account}
        <p class="text-muted-foreground">
            Вы вошли как сервисный администратор — у этого входа нет профиля.
        </p>
    {:else}
        <section class="mb-8 flex items-center gap-4">
            <Avatar.Root class="size-20 text-2xl">
                {#if account.avatarId}
                    <Avatar.Image src={avatarUrl(account.avatarId)} alt="" />
                {/if}
                <Avatar.Fallback>{initials(account)}</Avatar.Fallback>
            </Avatar.Root>
            <div class="flex flex-col gap-2">
                <div class="flex flex-wrap gap-2">
                    <Button
                        variant="outline"
                        size="sm"
                        disabled={savingAvatar}
                        onclick={() => avatarInput?.click()}
                    >
                        {#if savingAvatar}<Spinner />{/if}
                        {account.avatarId ? "Заменить фото" : "Загрузить фото"}
                    </Button>
                    {#if account.avatarId}
                        <Button
                            variant="ghost"
                            size="sm"
                            disabled={savingAvatar}
                            onclick={() => (deleteAvatarOpen = true)}
                        >
                            Удалить
                        </Button>
                    {/if}
                </div>
                <p class="text-xs text-muted-foreground">
                    PNG, JPEG, GIF или WebP до 2 МБ
                </p>
                <input
                    bind:this={avatarInput}
                    type="file"
                    accept={AVATAR_ACCEPT}
                    class="hidden"
                    onchange={onAvatarSelected}
                />
            </div>
        </section>

        <section class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">Личные данные</h2>
                <p class="text-sm text-muted-foreground">
                    Email: {account.email}
                </p>
            </div>
            <form onsubmit={saveProfile} class="flex flex-col gap-4">
                <Field.Group class="grid gap-4 sm:grid-cols-2">
                    <Field.Field data-invalid={!!profileErrors.lastName}>
                        <Field.Label for="lastName">Фамилия</Field.Label>
                        <Input
                            id="lastName"
                            required
                            bind:value={profile.lastName}
                        />
                        <Field.Error>{profileErrors.lastName}</Field.Error>
                    </Field.Field>
                    <Field.Field data-invalid={!!profileErrors.firstName}>
                        <Field.Label for="firstName">Имя</Field.Label>
                        <Input
                            id="firstName"
                            required
                            bind:value={profile.firstName}
                        />
                        <Field.Error>{profileErrors.firstName}</Field.Error>
                    </Field.Field>
                    <Field.Field>
                        <Field.Label for="middleName">Отчество</Field.Label>
                        <Input
                            id="middleName"
                            bind:value={profile.middleName}
                        />
                    </Field.Field>
                    <Field.Field data-invalid={!!profileErrors.phone}>
                        <Field.Label for="phone">Телефон</Field.Label>
                        <Input
                            id="phone"
                            type="tel"
                            bind:value={profile.phone}
                        />
                        <Field.Error>{profileErrors.phone}</Field.Error>
                    </Field.Field>
                    <Field.Field data-invalid={!!profileErrors.gender}>
                        <Field.Label for="gender">Пол</Field.Label>
                        <NativeSelect.Root
                            id="gender"
                            class="w-full"
                            bind:value={profile.gender}
                        >
                            <NativeSelect.Option value="">
                                Не указан
                            </NativeSelect.Option>
                            <NativeSelect.Option value="Female">
                                Женский
                            </NativeSelect.Option>
                            <NativeSelect.Option value="Male">
                                Мужской
                            </NativeSelect.Option>
                        </NativeSelect.Root>
                        <Field.Description>
                            Тогда в запросе доступа не нужно указывать, мама вы
                            или папа. Подтягивается из Яндекс ID.
                        </Field.Description>
                        <Field.Error>{profileErrors.gender}</Field.Error>
                    </Field.Field>
                </Field.Group>
                <div>
                    <Button type="submit" disabled={savingProfile}>
                        {#if savingProfile}<Spinner />{/if}
                        Сохранить
                    </Button>
                </div>
            </form>
        </section>

        <Separator class="my-8" />

        <section class="flex flex-col gap-4">
            <div>
                <h2 class="text-lg font-semibold">Ник</h2>
                <p class="text-sm text-muted-foreground">
                    Необязательно. Если не задан, подставится ник из Telegram
                    или логин Яндекса при входе через них.
                </p>
            </div>
            <form onsubmit={saveUsername} class="flex max-w-sm flex-col gap-4">
                <Field.Field data-invalid={!!usernameError}>
                    <Field.Label for="username">Ник</Field.Label>
                    <div class="relative">
                        <span
                            class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground"
                            aria-hidden="true">@</span
                        >
                        <Input
                            id="username"
                            class="pl-7"
                            autocomplete="username"
                            maxlength={33}
                            bind:value={username}
                        />
                    </div>
                    <Field.Description>
                        3–32 символа: латиница, цифры, «_», «.», «-».
                    </Field.Description>
                    <Field.Error>{usernameError}</Field.Error>
                </Field.Field>
                <div>
                    <Button type="submit" disabled={savingUsername}>
                        {#if savingUsername}<Spinner />{/if}
                        Сохранить
                    </Button>
                </div>
            </form>
        </section>

        <Separator class="my-8" />

        <section class="flex flex-col gap-4">
            <h2 class="text-lg font-semibold">Способы входа</h2>
            <ul class="divide-y rounded-lg border">
                <li class="flex items-center gap-3 p-3">
                    <KeyRoundIcon class="size-4 text-muted-foreground" />
                    <span class="flex-1">Email и пароль</span>
                    {#if hasPassword}
                        <Badge variant="secondary">Подключён</Badge>
                    {:else}
                        <Badge variant="outline">Пароль не задан</Badge>
                    {/if}
                </li>
                {#each data.providers as provider (provider.name)}
                    {@const linked = account.loginProviders.includes(
                        provider.name,
                    )}
                    <li class="flex flex-wrap items-center gap-3 p-3">
                        <LinkIcon class="size-4 text-muted-foreground" />
                        <span class="flex-1">{provider.displayName}</span>
                        {#if linked}
                            <Button
                                variant="ghost"
                                size="sm"
                                disabled={!hasPassword}
                                title={hasPassword
                                    ? undefined
                                    : "Сначала задайте пароль, иначе нечем будет войти"}
                                onclick={() => {
                                    unlinking = provider.name;
                                    unlinkOpen = true;
                                }}
                            >
                                Отвязать
                            </Button>
                        {:else}
                            <Button
                                variant="outline"
                                size="sm"
                                href={linkProviderUrl(provider.name)}
                                data-sveltekit-reload
                            >
                                Привязать
                            </Button>
                        {/if}
                    </li>
                {/each}
            </ul>
        </section>

        <Separator class="my-8" />

        <section class="flex flex-col gap-4">
            <h2 class="text-lg font-semibold">
                {hasPassword ? "Смена пароля" : "Задать пароль"}
            </h2>
            <form onsubmit={savePassword} class="flex max-w-sm flex-col gap-4">
                <Field.Group>
                    {#if hasPassword}
                        <Field.Field
                            data-invalid={!!passwordErrors.currentPassword}
                        >
                            <Field.Label for="current">
                                Текущий пароль
                            </Field.Label>
                            <Input
                                id="current"
                                type="password"
                                autocomplete="current-password"
                                required
                                bind:value={password.current}
                            />
                            <Field.Error>
                                {passwordErrors.currentPassword}
                            </Field.Error>
                        </Field.Field>
                    {/if}
                    <Field.Field data-invalid={!!passwordErrors.newPassword}>
                        <Field.Label for="next">Новый пароль</Field.Label>
                        <Input
                            id="next"
                            type="password"
                            autocomplete="new-password"
                            required
                            bind:value={password.next}
                        />
                        <Field.Error>{passwordErrors.newPassword}</Field.Error>
                    </Field.Field>
                    <Field.Field data-invalid={!!passwordErrors.repeat}>
                        <Field.Label for="repeat">Повторите пароль</Field.Label>
                        <Input
                            id="repeat"
                            type="password"
                            autocomplete="new-password"
                            required
                            bind:value={password.repeat}
                        />
                        <Field.Error>{passwordErrors.repeat}</Field.Error>
                    </Field.Field>
                </Field.Group>
                <div>
                    <Button type="submit" disabled={savingPassword}>
                        {#if savingPassword}<Spinner />{/if}
                        {hasPassword ? "Сменить пароль" : "Задать пароль"}
                    </Button>
                </div>
            </form>
        </section>
    {/if}
</div>

<ConfirmDialog
    bind:open={unlinkOpen}
    title="Отвязать внешний вход?"
    description="Входить можно будет только по email и паролю."
    confirmText="Отвязать"
    destructive
    onConfirm={async () => {
        if (!unlinking) return;
        await unlinkProvider(unlinking);
        await invalidateAll();
        toast.success("Отвязано");
    }}
/>

<ConfirmDialog
    bind:open={deleteAvatarOpen}
    title="Удалить аватарку?"
    confirmText="Удалить"
    destructive
    onConfirm={async () => {
        await deleteAvatar();
        await Promise.all([invalidateAll(), session.load(true)]);
        toast.success("Аватарка удалена");
    }}
/>
