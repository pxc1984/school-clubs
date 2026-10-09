<script lang="ts">
    import { toast } from "svelte-sonner";
    import { errorMessage } from "$lib/api/client";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";

    /** Диалог с одним текстовым полем: название задачи, причина отказа и т.п. */
    let {
        open = $bindable(false),
        title,
        description,
        label,
        value = "",
        placeholder,
        required = false,
        maxlength = 200,
        submitText = "Сохранить",
        onSubmit,
    }: {
        open?: boolean;
        title: string;
        description?: string;
        label: string;
        value?: string;
        placeholder?: string;
        required?: boolean;
        maxlength?: number;
        submitText?: string;
        onSubmit: (value: string) => Promise<void> | void;
    } = $props();

    let text = $state("");
    let busy = $state(false);

    $effect.pre(() => {
        if (open) text = value;
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        busy = true;
        try {
            await onSubmit(text.trim());
            open = false;
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="sm:max-w-md">
        <Dialog.Header>
            <Dialog.Title>{title}</Dialog.Title>
            {#if description}
                <Dialog.Description>{description}</Dialog.Description>
            {/if}
        </Dialog.Header>
        <form id="text-prompt" onsubmit={submit}>
            <Field.Field>
                <Field.Label for="text-prompt-input">{label}</Field.Label>
                <Input
                    id="text-prompt-input"
                    bind:value={text}
                    {placeholder}
                    {required}
                    {maxlength}
                />
            </Field.Field>
        </form>
        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button
                type="submit"
                form="text-prompt"
                disabled={busy || (required && !text.trim())}
            >
                {#if busy}<Spinner />{/if}
                {submitText}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
