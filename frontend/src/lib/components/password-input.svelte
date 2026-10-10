<script lang="ts">
    import EyeIcon from "@lucide/svelte/icons/eye";
    import EyeOffIcon from "@lucide/svelte/icons/eye-off";
    import type { HTMLInputAttributes } from "svelte/elements";
    import { Input } from "$lib/components/ui/input";

    // Поле пароля с кнопкой «показать»: на телефоне пароль часто вводят с опечаткой.
    let {
        value = $bindable(""),
        ...rest
    }: Omit<HTMLInputAttributes, "type" | "value" | "files"> & {
        value?: string;
    } = $props();

    let visible = $state(false);
</script>

<div class="relative">
    <Input
        {...rest}
        type={visible ? "text" : "password"}
        bind:value
        class="pr-9"
    />
    <button
        type="button"
        class="absolute inset-y-0 right-0 flex w-9 items-center justify-center rounded-r-md text-muted-foreground outline-none hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/50"
        aria-label={visible ? "Скрыть пароль" : "Показать пароль"}
        aria-pressed={visible}
        onclick={() => (visible = !visible)}
    >
        {#if visible}<EyeOffIcon class="size-4" />{:else}<EyeIcon
                class="size-4"
            />{/if}
    </button>
</div>
