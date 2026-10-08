<script lang="ts">
    import { onMount } from "svelte";
    import {
        externalLoginUrl,
        listExternalProviders,
        type ExternalProviderDto,
    } from "$lib/api/auth";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";

    let {
        returnUrl,
        verb = "Войти",
        separator = "или",
    }: { returnUrl: string; verb?: string; separator?: string } = $props();

    let providers = $state<ExternalProviderDto[]>([]);

    onMount(async () => {
        // нет провайдеров или сервер недоступен — просто не показываем блок
        providers = await listExternalProviders().catch(() => []);
    });
</script>

{#if providers.length}
    <div class="flex flex-col gap-2">
        {#each providers as provider (provider.name)}
            <!-- полная навигация: OIDC-флоу ведёт backend -->
            <Button
                href={externalLoginUrl(provider.name, returnUrl)}
                variant="outline"
                class="w-full"
                data-sveltekit-reload
            >
                {#if provider.name === "yandex"}
                    <span
                        class="flex size-5 items-center justify-center rounded-full bg-[#fc3f1d] text-xs font-bold text-white"
                        aria-hidden="true">Я</span
                    >
                {/if}
                {verb} через {provider.displayName}
            </Button>
        {/each}
    </div>
    <Field.Separator class="my-6">{separator}</Field.Separator>
{/if}
