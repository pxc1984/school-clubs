<script lang="ts">
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { register } from "$lib/api/auth";
    import {
        errorMessage,
        fieldErrors,
        safeReturnUrl,
        statusOf,
    } from "$lib/api/client";
    import ExternalLogins from "$lib/components/external-logins.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import ConsentCheckbox from "$lib/components/consent-checkbox.svelte";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { ACCOUNT_CONSENT_PREFIX } from "$lib/legal";
    import { session } from "$lib/session.svelte";

    const MIN_PASSWORD_LENGTH = 8;

    // например, ссылка-приглашение от админа: после регистрации вернёмся на неё
    const returnUrl = $derived(
        safeReturnUrl(page.url.searchParams.get("returnUrl")),
    );

    let form = $state({
        lastName: "",
        firstName: "",
        middleName: "",
        email: "",
        phone: "",
        password: "",
    });
    let consent = $state(false);
    let busy = $state(false);
    let error = $state<string | null>(null);
    let errors = $state<Record<string, string>>({});

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        errors = {};
        error = null;
        if (form.password.length < MIN_PASSWORD_LENGTH) {
            errors = {
                password: `Не короче ${MIN_PASSWORD_LENGTH} символов`,
            };
            return;
        }
        if (!consent) {
            errors = {
                acceptPersonalDataPolicy: "Без согласия регистрация невозможна",
            };
            return;
        }

        busy = true;
        try {
            await register({
                lastName: form.lastName.trim(),
                firstName: form.firstName.trim(),
                middleName: form.middleName.trim() || null,
                email: form.email.trim(),
                phone: form.phone.trim() || null,
                password: form.password,
                acceptPersonalDataPolicy: consent,
            });
            await session.load(true);
            await goto(returnUrl ?? "/parent");
        } catch (e) {
            errors = fieldErrors(e);
            error =
                statusOf(e) === 409
                    ? "Аккаунт с таким email уже есть. Войдите или обратитесь к администратору."
                    : Object.keys(errors).length
                      ? null
                      : errorMessage(e);
        } finally {
            busy = false;
        }
    }
</script>

<svelte:head><title>Регистрация — Кружки 218</title></svelte:head>

<h1 class="mb-1 text-2xl font-semibold tracking-tight">Регистрация</h1>
<p class="mb-6 text-sm text-muted-foreground">
    После регистрации запросите доступ к отметкам ребёнка.
</p>

<ExternalLogins
    returnUrl={returnUrl ?? "/parent"}
    verb="Зарегистрироваться"
    separator="или по email"
/>

<form onsubmit={submit} class="flex flex-col gap-6" novalidate>
    {#if error}
        <Alert.Root variant="destructive">
            <Alert.Description>{error}</Alert.Description>
        </Alert.Root>
    {/if}

    <Field.Group>
        <Field.Field data-invalid={!!errors.lastName}>
            <Field.Label for="lastName">Фамилия</Field.Label>
            <Input
                id="lastName"
                autocomplete="family-name"
                required
                bind:value={form.lastName}
            />
            <Field.Error>{errors.lastName}</Field.Error>
        </Field.Field>
        <Field.Field data-invalid={!!errors.firstName}>
            <Field.Label for="firstName">Имя</Field.Label>
            <Input
                id="firstName"
                autocomplete="given-name"
                required
                bind:value={form.firstName}
            />
            <Field.Error>{errors.firstName}</Field.Error>
        </Field.Field>
        <Field.Field>
            <Field.Label for="middleName">Отчество</Field.Label>
            <Input
                id="middleName"
                autocomplete="additional-name"
                bind:value={form.middleName}
            />
        </Field.Field>
        <Field.Field data-invalid={!!errors.email}>
            <Field.Label for="email">Email</Field.Label>
            <Input
                id="email"
                type="email"
                autocomplete="email"
                required
                bind:value={form.email}
            />
            <Field.Error>{errors.email}</Field.Error>
        </Field.Field>
        <Field.Field data-invalid={!!errors.phone}>
            <Field.Label for="phone">Телефон</Field.Label>
            <Input
                id="phone"
                type="tel"
                autocomplete="tel"
                placeholder="+7 900 000-00-00"
                bind:value={form.phone}
            />
            <Field.Description>Необязательно</Field.Description>
            <Field.Error>{errors.phone}</Field.Error>
        </Field.Field>
        <Field.Field data-invalid={!!errors.password}>
            <Field.Label for="password">Пароль</Field.Label>
            <Input
                id="password"
                type="password"
                autocomplete="new-password"
                required
                minlength={MIN_PASSWORD_LENGTH}
                bind:value={form.password}
            />
            <Field.Description>
                Не короче {MIN_PASSWORD_LENGTH} символов
            </Field.Description>
            <Field.Error>{errors.password}</Field.Error>
        </Field.Field>
        <ConsentCheckbox
            id="consent"
            bind:checked={consent}
            prefix={ACCOUNT_CONSENT_PREFIX}
            error={errors.acceptPersonalDataPolicy}
        />
    </Field.Group>

    <Button type="submit" class="w-full" disabled={busy}>
        {#if busy}<Spinner />{/if}
        Зарегистрироваться
    </Button>
</form>

<p class="mt-6 text-center text-sm text-muted-foreground">
    Уже есть аккаунт?
    <a
        href={returnUrl
            ? `/login?returnUrl=${encodeURIComponent(returnUrl)}`
            : "/login"}
        class="font-medium text-foreground underline">Войти</a
    >
</p>
