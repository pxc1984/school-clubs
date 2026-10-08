<script lang="ts">
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { acceptConsent } from "$lib/api/account";
    import { logout } from "$lib/api/auth";
    import { errorMessage, safeReturnUrl } from "$lib/api/client";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import ConsentCheckbox from "$lib/components/consent-checkbox.svelte";
    import { Spinner } from "$lib/components/ui/spinner";
    import { ACCOUNT_CONSENT_PREFIX } from "$lib/legal";
    import { session } from "$lib/session.svelte";

    let consent = $state(false);
    let busy = $state(false);
    let error = $state<string | null>(null);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!consent) return;
        busy = true;
        error = null;
        try {
            await acceptConsent();
            await session.load(true);
            await goto(
                safeReturnUrl(page.url.searchParams.get("returnUrl")) ??
                    session.homeHref(),
            );
        } catch (e) {
            error = errorMessage(e);
        } finally {
            busy = false;
        }
    }

    async function decline() {
        await logout().catch(() => {});
        session.clear();
        await goto("/login");
    }
</script>

<svelte:head><title>Согласие — Кружки 218</title></svelte:head>

<h1 class="mb-1 text-2xl font-semibold tracking-tight">
    Согласие на обработку данных
</h1>
<p class="mb-6 text-sm text-muted-foreground">
    Чтобы продолжить, примите текущую версию политики обработки персональных
    данных. Без согласия мы не можем показывать отметки.
</p>

<form onsubmit={submit} class="flex flex-col gap-6">
    {#if error}
        <Alert.Root variant="destructive">
            <Alert.Description>{error}</Alert.Description>
        </Alert.Root>
    {/if}

    <ConsentCheckbox
        id="consent"
        bind:checked={consent}
        prefix={ACCOUNT_CONSENT_PREFIX}
    />

    <div class="flex flex-col gap-2">
        <Button type="submit" disabled={!consent || busy}>
            {#if busy}<Spinner />{/if}
            Продолжить
        </Button>
        <Button variant="ghost" onclick={decline}>Не согласен, выйти</Button>
    </div>
</form>
