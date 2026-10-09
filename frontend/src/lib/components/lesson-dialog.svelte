<script lang="ts">
    import { toast } from "svelte-sonner";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import {
        createLesson,
        updateLesson,
        type LessonDto,
    } from "$lib/api/teacher";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { formatTime, today } from "$lib/format";

    const MAX_TASKS = 30;

    /** Создание в группе или правка существующего занятия */
    type Mode =
        | { kind: "create"; groupId: string; taskCount: number }
        | { kind: "edit"; lesson: LessonDto };

    let {
        open = $bindable(false),
        mode,
        onSaved,
    }: {
        open?: boolean;
        mode: Mode;
        onSaved: (lesson: LessonDto) => Promise<void> | void;
    } = $props();

    let date = $state("");
    let time = $state("");
    let topic = $state("");
    let taskCount = $state(5);
    let errors = $state<Record<string, string>>({});
    let busy = $state(false);

    // заполняем форму при каждом открытии
    $effect.pre(() => {
        if (!open) return;
        errors = {};
        if (mode.kind === "create") {
            date = today();
            time = "";
            topic = "";
            taskCount = mode.taskCount;
        } else {
            date = mode.lesson.date;
            time = formatTime(mode.lesson.time);
            topic = mode.lesson.topic ?? "";
        }
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        errors = {};
        if (!date) errors.date = "Укажите дату";
        if (
            mode.kind === "create" &&
            (!Number.isInteger(taskCount) ||
                taskCount < 1 ||
                taskCount > MAX_TASKS)
        )
            errors.taskCount = `От 1 до ${MAX_TASKS}`;
        if (Object.keys(errors).length) return;

        busy = true;
        try {
            const base = {
                date,
                time: time || null,
                topic: topic.trim() || null,
            };
            const lesson =
                mode.kind === "create"
                    ? await createLesson(mode.groupId, { ...base, taskCount })
                    : await updateLesson(mode.lesson.id, base);
            await onSaved(lesson);
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
            <Dialog.Title>
                {mode.kind === "create" ? "Новое занятие" : "Занятие"}
            </Dialog.Title>
            {#if mode.kind === "create"}
                <Dialog.Description>
                    Журнал создастся сразу с задачами — их можно добавить или
                    удалить позже.
                </Dialog.Description>
            {/if}
        </Dialog.Header>

        <form id="lesson-form" onsubmit={submit} novalidate>
            <Field.Group>
                <div class="grid grid-cols-2 gap-4">
                    <Field.Field data-invalid={!!errors.date}>
                        <Field.Label for="lesson-date">Дата</Field.Label>
                        <Input
                            id="lesson-date"
                            type="date"
                            required
                            bind:value={date}
                            aria-invalid={!!errors.date}
                        />
                        <Field.Error>{errors.date}</Field.Error>
                    </Field.Field>
                    <Field.Field data-invalid={!!errors.time}>
                        <Field.Label for="lesson-time">Время</Field.Label>
                        <Input id="lesson-time" type="time" bind:value={time} />
                        <Field.Error>{errors.time}</Field.Error>
                    </Field.Field>
                </div>
                <Field.Field data-invalid={!!errors.topic}>
                    <Field.Label for="lesson-topic">Тема</Field.Label>
                    <Input
                        id="lesson-topic"
                        maxlength={200}
                        placeholder="Необязательно"
                        bind:value={topic}
                    />
                    <Field.Error>{errors.topic}</Field.Error>
                </Field.Field>
                {#if mode.kind === "create"}
                    <Field.Field data-invalid={!!errors.taskCount}>
                        <Field.Label for="lesson-tasks">
                            Число задач
                        </Field.Label>
                        <Input
                            id="lesson-tasks"
                            type="number"
                            min={1}
                            max={MAX_TASKS}
                            class="w-24"
                            bind:value={taskCount}
                            aria-invalid={!!errors.taskCount}
                        />
                        <Field.Description>
                            По умолчанию — как на прошлом занятии
                        </Field.Description>
                        <Field.Error>{errors.taskCount}</Field.Error>
                    </Field.Field>
                {/if}
            </Field.Group>
        </form>

        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button type="submit" form="lesson-form" disabled={busy}>
                {#if busy}<Spinner />{/if}
                {mode.kind === "create" ? "Создать" : "Сохранить"}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
