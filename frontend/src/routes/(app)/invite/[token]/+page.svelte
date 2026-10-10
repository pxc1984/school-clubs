<script lang="ts">
    import { onMount } from "svelte";
    import { toast } from "svelte-sonner";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { errorMessage, statusOf } from "$lib/api/client";
    import { acceptChildInvite } from "$lib/api/parent";
    import { Button } from "$lib/components/ui/button";
    import * as Empty from "$lib/components/ui/empty";
    import { Spinner } from "$lib/components/ui/spinner";
    import { fullName } from "$lib/format";
    import { session } from "$lib/session.svelte";

    // Вход, регистрация и согласие уже позади — их обеспечивает гвард (app)
    // с returnUrl на эту страницу. Привязка — POST, поэтому не в load: тот
    // срабатывает и при предзагрузке ссылки.
    let error = $state<string | null>(null);

    onMount(async () => {
        try {
            const { student, alreadyLinked } = await acceptChildInvite(
                page.params.token!,
            );
            // роль «Родитель» могла появиться только что
            await session.load(true);
            toast.success(
                alreadyLinked
                    ? `${fullName(student)} уже в вашем кабинете`
                    : `${fullName(student)} добавлен(а) в ваш кабинет`,
            );
            await goto(`/parent/children/${student.id}`, {
                replaceState: true,
            });
        } catch (e) {
            const status = statusOf(e);
            error =
                status === 404 || status === 410
                    ? "Ссылка недействительна: её уже использовали, отозвали или срок действия истёк. Попросите у администратора кружков новую."
                    : status === 403
                      ? "Войдите под аккаунтом родителя, чтобы перейти по ссылке."
                      : errorMessage(e);
        }
    });
</script>

<svelte:head><title>Приглашение — Кружки 218</title></svelte:head>

{#if error}
    <Empty.Root class="border">
        <Empty.Header>
            <Empty.Title>Не удалось добавить ребёнка</Empty.Title>
            <Empty.Description>{error}</Empty.Description>
        </Empty.Header>
        <Empty.Content>
            <Button href={session.homeHref()} variant="outline">
                В кабинет
            </Button>
        </Empty.Content>
    </Empty.Root>
{:else}
    <div
        class="flex items-center justify-center gap-2 py-16 text-muted-foreground"
    >
        <Spinner />
        Добавляем ребёнка…
    </div>
{/if}
