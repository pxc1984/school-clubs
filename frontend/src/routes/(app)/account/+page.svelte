<script lang="ts">
    import { toast } from "svelte-sonner";
    import KeyRoundIcon from "@lucide/svelte/icons/key-round";
    import LinkIcon from "@lucide/svelte/icons/link";
    import { invalidateAll } from "$app/navigation";
    import {
        changePassword,
        unlinkProvider,
        updateAccount,
    } from "$lib/api/account";
    import { externalLoginUrl } from "$lib/api/auth";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Separator } from "$lib/components/ui/separator";
    import { Spinner } from "$lib/components/ui/spinner";
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
    });
    $effect.pre(() => {
        if (!account) return;
        profile = {
            lastName: account.lastName,
            firstName: account.firstName,
            middleName: account.middleName ?? "",
            phone: account.phone ?? "",
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
                                href={externalLoginUrl(
                                    provider.name,
                                    "/account",
                                )}
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
