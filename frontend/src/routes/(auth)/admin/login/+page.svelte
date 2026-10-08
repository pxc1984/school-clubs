<script lang="ts">
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { adminLogin } from "$lib/api/auth";
    import { errorMessage, safeReturnUrl, statusOf } from "$lib/api/client";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { session } from "$lib/session.svelte";

    let password = $state("");
    let busy = $state(false);
    let error = $state<string | null>(null);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        busy = true;
        error = null;
        try {
            await adminLogin(password);
            await session.load(true);
            await goto(
                safeReturnUrl(page.url.searchParams.get("returnUrl")) ??
                    "/admin",
            );
        } catch (e) {
            const status = statusOf(e);
            error =
                status === 401
                    ? "Неверный пароль"
                    : status === 429
                      ? "Слишком много попыток. Подождите минуту."
                      : status === 404
                        ? "Сервисный вход выключен"
                        : errorMessage(e);
        } finally {
            busy = false;
        }
    }
</script>

<svelte:head><title>Сервисный вход — Кружки 218</title></svelte:head>

<h1 class="mb-1 text-2xl font-semibold tracking-tight">Сервисный вход</h1>
<p class="mb-6 text-sm text-muted-foreground">
    Для первичной настройки и аварийного доступа. Обычно админ входит своим
    аккаунтом на <a href="/login" class="underline">странице входа</a>.
</p>

<form onsubmit={submit} class="flex flex-col gap-6">
    {#if error}
        <Alert.Root variant="destructive">
            <Alert.Description>{error}</Alert.Description>
        </Alert.Root>
    {/if}
    <Field.Field>
        <Field.Label for="password">Пароль администратора</Field.Label>
        <Input
            id="password"
            type="password"
            autocomplete="current-password"
            required
            bind:value={password}
        />
    </Field.Field>
    <Button type="submit" disabled={busy}>
        {#if busy}<Spinner />{/if}
        Войти
    </Button>
</form>
