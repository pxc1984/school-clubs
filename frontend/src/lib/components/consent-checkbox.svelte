<script lang="ts">
    import { Checkbox } from "$lib/components/ui/checkbox";
    import * as Field from "$lib/components/ui/field";
    import { POLICY_LINK_TEXT } from "$lib/legal";

    let {
        id,
        checked = $bindable(false),
        prefix,
        error,
    }: {
        id: string;
        checked?: boolean;
        /** Текст до ссылки на политику */
        prefix: string;
        error?: string;
    } = $props();
</script>

<Field.Field orientation="horizontal" data-invalid={!!error}>
    <Checkbox {id} bind:checked aria-invalid={!!error} />
    <Field.Content>
        <Field.Label for={id} class="block leading-snug font-normal">
            {prefix}
            <a href="/privacy" target="_blank" class="underline">
                {POLICY_LINK_TEXT}</a
            >
        </Field.Label>
        <Field.Error>{error}</Field.Error>
    </Field.Content>
</Field.Field>
