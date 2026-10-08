<script lang="ts">
    import type { Snippet } from "svelte";
    import { toast } from "svelte-sonner";
    import { errorMessage } from "$lib/api/client";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { buttonVariants } from "$lib/components/ui/button";
    import { Spinner } from "$lib/components/ui/spinner";

    let {
        open = $bindable(false),
        title,
        description,
        confirmText = "Подтвердить",
        destructive = false,
        disabled = false,
        onConfirm,
        children,
    }: {
        open?: boolean;
        title: string;
        description?: string;
        confirmText?: string;
        destructive?: boolean;
        disabled?: boolean;
        /** Ошибка показывается тостом, диалог остаётся открытым */
        onConfirm: () => Promise<void> | void;
        children?: Snippet;
    } = $props();

    let busy = $state(false);

    async function confirm(event: MouseEvent) {
        event.preventDefault();
        busy = true;
        try {
            await onConfirm();
            open = false;
        } catch (error) {
            toast.error(errorMessage(error));
        } finally {
            busy = false;
        }
    }
</script>

<AlertDialog.Root bind:open>
    <AlertDialog.Content>
        <AlertDialog.Header>
            <AlertDialog.Title>{title}</AlertDialog.Title>
            {#if description}
                <AlertDialog.Description>{description}</AlertDialog.Description>
            {/if}
        </AlertDialog.Header>
        {@render children?.()}
        <AlertDialog.Footer>
            <AlertDialog.Cancel disabled={busy}>Отмена</AlertDialog.Cancel>
            <AlertDialog.Action
                class={destructive
                    ? buttonVariants({ variant: "destructive" })
                    : undefined}
                disabled={busy || disabled}
                onclick={confirm}
            >
                {#if busy}<Spinner />{/if}
                {confirmText}
            </AlertDialog.Action>
        </AlertDialog.Footer>
    </AlertDialog.Content>
</AlertDialog.Root>
