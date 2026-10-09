<script lang="ts" module>
    import type { PersonName } from "$lib/api/types";

    export type StudentForm = PersonName & { grade: string | null };
</script>

<script lang="ts">
    import { toast } from "svelte-sonner";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import type { StudentDto } from "$lib/api/types";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";

    let {
        open = $bindable(false),
        title = "Новый ученик",
        description,
        student = null,
        submitText = "Добавить",
        onSubmit,
    }: {
        open?: boolean;
        title?: string;
        description?: string;
        /** Для правки: исходные данные */
        student?: StudentDto | null;
        submitText?: string;
        onSubmit: (form: StudentForm) => Promise<void> | void;
    } = $props();

    let lastName = $state("");
    let firstName = $state("");
    let middleName = $state("");
    let grade = $state("");
    let errors = $state<Record<string, string>>({});
    let busy = $state(false);

    $effect.pre(() => {
        if (!open) return;
        errors = {};
        lastName = student?.lastName ?? "";
        firstName = student?.firstName ?? "";
        middleName = student?.middleName ?? "";
        grade = student?.grade ?? "";
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        errors = {};
        if (!lastName.trim()) errors.lastName = "Укажите фамилию";
        if (!firstName.trim()) errors.firstName = "Укажите имя";
        if (Object.keys(errors).length) return;

        busy = true;
        try {
            await onSubmit({
                lastName: lastName.trim(),
                firstName: firstName.trim(),
                middleName: middleName.trim() || null,
                grade: grade.trim() || null,
            });
            open = false;
        } catch (e) {
            errors = fieldErrors(e);
            if (!Object.keys(errors).length) toast.error(errorMessage(e));
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

        <form id="student-form" onsubmit={submit} novalidate>
            <Field.Group>
                <Field.Field data-invalid={!!errors.lastName}>
                    <Field.Label for="student-last">Фамилия</Field.Label>
                    <Input
                        id="student-last"
                        autocomplete="off"
                        bind:value={lastName}
                        aria-invalid={!!errors.lastName}
                    />
                    <Field.Error>{errors.lastName}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!errors.firstName}>
                    <Field.Label for="student-first">Имя</Field.Label>
                    <Input
                        id="student-first"
                        autocomplete="off"
                        bind:value={firstName}
                        aria-invalid={!!errors.firstName}
                    />
                    <Field.Error>{errors.firstName}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!errors.middleName}>
                    <Field.Label for="student-middle">Отчество</Field.Label>
                    <Input
                        id="student-middle"
                        autocomplete="off"
                        placeholder="Необязательно"
                        bind:value={middleName}
                    />
                    <Field.Error>{errors.middleName}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!errors.grade}>
                    <Field.Label for="student-grade">Класс</Field.Label>
                    <Input
                        id="student-grade"
                        class="w-24"
                        maxlength={10}
                        placeholder="7Б"
                        bind:value={grade}
                    />
                    <Field.Error>{errors.grade}</Field.Error>
                </Field.Field>
            </Field.Group>
        </form>

        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button type="submit" form="student-form" disabled={busy}>
                {#if busy}<Spinner />{/if}
                {submitText}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
