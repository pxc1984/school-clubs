<script lang="ts">
    import { toast } from "svelte-sonner";
    import {
        createGroup,
        updateGroup,
        type AdminGroupDetailsDto,
    } from "$lib/api/admin";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import { Button } from "$lib/components/ui/button";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { Textarea } from "$lib/components/ui/textarea";

    let {
        open = $bindable(false),
        group = null,
        onSaved,
    }: {
        open?: boolean;
        /** null — новая группа */
        group?: AdminGroupDetailsDto | null;
        onSaved: (group: AdminGroupDetailsDto) => Promise<void> | void;
    } = $props();

    let title = $state("");
    let description = $state("");
    let archived = $state(false);
    let errors = $state<Record<string, string>>({});
    let busy = $state(false);

    $effect.pre(() => {
        if (!open) return;
        errors = {};
        title = group?.title ?? "";
        description = group?.description ?? "";
        archived = group?.isArchived ?? false;
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        errors = {};
        if (!title.trim()) {
            errors.title = "Укажите название";
            return;
        }
        busy = true;
        try {
            const request = {
                title: title.trim(),
                description: description.trim() || null,
            };
            const saved = group
                ? await updateGroup(group.id, {
                      ...request,
                      isArchived: archived,
                  })
                : await createGroup(request);
            await onSaved(saved);
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
            <Dialog.Title>{group ? "Группа" : "Новая группа"}</Dialog.Title>
        </Dialog.Header>
        <form id="group-form" onsubmit={submit} novalidate>
            <Field.Group>
                <Field.Field data-invalid={!!errors.title}>
                    <Field.Label for="group-title">Название</Field.Label>
                    <Input
                        id="group-title"
                        maxlength={200}
                        placeholder="Шахматы, 5–6 классы"
                        bind:value={title}
                        aria-invalid={!!errors.title}
                    />
                    <Field.Error>{errors.title}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!errors.description}>
                    <Field.Label for="group-description">Описание</Field.Label>
                    <Textarea
                        id="group-description"
                        rows={3}
                        maxlength={1000}
                        placeholder="Расписание, кабинет — по желанию"
                        bind:value={description}
                    />
                    <Field.Error>{errors.description}</Field.Error>
                </Field.Field>
                {#if group}
                    <Field.Field orientation="horizontal">
                        <Checkbox id="group-archived" bind:checked={archived} />
                        <Field.Content>
                            <Field.Label
                                for="group-archived"
                                class="font-normal"
                            >
                                В архиве
                            </Field.Label>
                            <Field.Description>
                                Группа пропадёт из списков, история отметок
                                сохранится.
                            </Field.Description>
                        </Field.Content>
                    </Field.Field>
                {/if}
            </Field.Group>
        </form>
        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button type="submit" form="group-form" disabled={busy}>
                {#if busy}<Spinner />{/if}
                {group ? "Сохранить" : "Создать"}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
