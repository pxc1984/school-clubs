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
                {:else if provider.name === "telegram"}
                    <svg
                        class="size-5 text-[#2aabee]"
                        viewBox="0 0 24 24"
                        fill="currentColor"
                        aria-hidden="true"
                    >
                        <path
                            d="M12 0a12 12 0 1 0 0 24 12 12 0 0 0 0-24Zm5.56 8.16-1.97 9.28c-.15.66-.54.82-1.09.51l-3-2.21-1.45 1.4c-.16.16-.3.3-.6.3l.21-3.05 5.56-5.02c.24-.21-.05-.33-.37-.12l-6.87 4.33-2.96-.92c-.64-.2-.66-.64.14-.95l11.57-4.46c.53-.2 1 .13.83.94Z"
                        />
                    </svg>
                {/if}
                {verb} через {provider.displayName}
            </Button>
        {/each}
    </div>
    <Field.Separator class="my-6">{separator}</Field.Separator>
{/if}
