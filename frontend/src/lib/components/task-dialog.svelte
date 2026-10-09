<script lang="ts">
    import { toast } from "svelte-sonner";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import { updateTask } from "$lib/api/teacher";
    import type { TaskDto } from "$lib/api/types";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";

    /** Как TaskNumber на backend: до 20 символов, без пробелов и двоеточий */
    const NUMBER_MAX_LENGTH = 20;

    let {
        open = $bindable(false),
        lessonId,
        task,
        onSaved,
    }: {
        open?: boolean;
        lessonId: string;
        task: TaskDto | null;
        onSaved: () => Promise<void> | void;
    } = $props();

    let number = $state("");
    let title = $state("");
    let errors = $state<Record<string, string>>({});
    let busy = $state(false);

    // заполняем форму при каждом открытии
    $effect.pre(() => {
        if (!open || !task) return;
        errors = {};
        number = task.number;
        title = task.title ?? "";
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!task) return;
        errors = {};
        const trimmed = number.trim();
        if (!trimmed) errors.number = "Укажите номер";
        else if (/[\s:]/.test(trimmed))
            errors.number = "Без пробелов и двоеточий";
        if (Object.keys(errors).length) return;

        busy = true;
        try {
            await updateTask(lessonId, task.id, {
                number: trimmed,
                title: title.trim() || null,
            });
            await onSaved();
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
            <Dialog.Title>Задача {task?.number ?? ""}</Dialog.Title>
            <Dialog.Description>
                Номер и название видны в заголовке столбца и родителям. Задачи
                идут по порядку номеров: 1, 1.1, 1.2, 2…
            </Dialog.Description>
        </Dialog.Header>

        <form id="task-form" onsubmit={submit} novalidate>
            <Field.Group>
                <Field.Field data-invalid={!!errors.number}>
                    <Field.Label for="task-number">Номер</Field.Label>
                    <Input
                        id="task-number"
                        class="w-32"
                        required
                        maxlength={NUMBER_MAX_LENGTH}
                        placeholder="1.1"
                        bind:value={number}
                        aria-invalid={!!errors.number}
                    />
                    <Field.Error>{errors.number}</Field.Error>
                </Field.Field>
                <Field.Field data-invalid={!!errors.title}>
                    <Field.Label for="task-title">Название</Field.Label>
                    <Input
                        id="task-title"
                        maxlength={200}
                        placeholder="Например, «Ладья на шахматной доске»"
                        bind:value={title}
                        aria-invalid={!!errors.title}
                    />
                    <Field.Error>{errors.title}</Field.Error>
                </Field.Field>
            </Field.Group>
        </form>

        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button type="submit" form="task-form" disabled={busy}>
                {#if busy}<Spinner />{/if}
                Сохранить
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
