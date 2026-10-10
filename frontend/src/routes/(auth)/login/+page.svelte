<script lang="ts">
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { login } from "$lib/api/auth";
    import { errorMessage, problemOf, safeReturnUrl } from "$lib/api/client";
    import ExternalLogins from "$lib/components/external-logins.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { session } from "$lib/session.svelte";

    const BLOCKED =
        "Аккаунт заблокирован. Обратитесь к администратору кружков.";

    const returnUrl = $derived(
        safeReturnUrl(page.url.searchParams.get("returnUrl")),
    );

    // backend возвращает сюда после неудачного внешнего входа
    const externalError = $derived.by(() => {
        switch (page.url.searchParams.get("error")) {
            case "external":
                return "Не удалось войти через внешний сервис. Попробуйте ещё раз.";
            case "blocked":
                return BLOCKED;
            default:
                return null;
        }
    });

    let email = $state("");
    let password = $state("");
    let busy = $state(false);
    let error = $state<string | null>(null);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        busy = true;
        error = null;
        try {
            await login({ email: email.trim(), password });
            const me = await session.load(true);
            if (me?.consentRequired)
                await goto(
                    returnUrl
                        ? `/consent?returnUrl=${encodeURIComponent(returnUrl)}`
                        : "/consent",
                );
            else await goto(returnUrl ?? session.homeHref());
        } catch (e) {
            const problem = problemOf(e);
            error =
                problem?.status === 401
                    ? "Неверный email или пароль"
                    : problem?.code === "blocked"
                      ? BLOCKED
                      : errorMessage(e);
        } finally {
            busy = false;
        }
    }
</script>

<svelte:head><title>Вход — Кружки 218</title></svelte:head>

<h1 class="mb-1 text-2xl font-semibold tracking-tight">Вход</h1>
<p class="mb-6 text-sm text-muted-foreground">
    Кабинет родителей и учителей школьных кружков
</p>

{#if error ?? externalError}
    <Alert.Root variant="destructive" class="mb-6">
        <Alert.Description>{error ?? externalError}</Alert.Description>
    </Alert.Root>
{/if}

<ExternalLogins returnUrl={returnUrl ?? "/"} separator="или по email" />

<form onsubmit={submit} class="flex flex-col gap-6">
    <Field.Group>
        <Field.Field>
            <Field.Label for="email">Email</Field.Label>
            <Input
                id="email"
                type="email"
                autocomplete="email"
                required
                bind:value={email}
            />
        </Field.Field>
        <Field.Field>
            <Field.Label for="password">Пароль</Field.Label>
            <Input
                id="password"
                type="password"
                autocomplete="current-password"
                required
                bind:value={password}
            />
        </Field.Field>
    </Field.Group>

    <Button type="submit" class="w-full" disabled={busy}>
        {#if busy}<Spinner />{/if}
        Войти
    </Button>
</form>

<p class="mt-6 text-center text-sm text-muted-foreground">
    Нет аккаунта?
    <a
        href={returnUrl
            ? `/register?returnUrl=${encodeURIComponent(returnUrl)}`
            : "/register"}
        class="font-medium text-foreground underline"
    >
        Зарегистрироваться
    </a>
</p>
<p class="mt-2 text-center text-xs text-muted-foreground">
    Забыли пароль? Обратитесь к администратору кружков.
</p>
