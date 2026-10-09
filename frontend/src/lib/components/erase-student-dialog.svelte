<script lang="ts">
    import { toast } from "svelte-sonner";
    import {
        eraseStudent,
        getErasurePreview,
        type StudentDetailsDto,
        type ErasurePreviewDto,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import * as Dialog from "$lib/components/ui/dialog";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import { Spinner } from "$lib/components/ui/spinner";
    import { countOf, fullName, today } from "$lib/format";
    import { PRIVACY_CONTACT_EMAIL } from "$lib/legal";

    /** Безвозвратное удаление ПДн ученика по заявлению (152-ФЗ) */
    let {
        open = $bindable(false),
        student,
        onErased,
    }: {
        open?: boolean;
        student: StudentDetailsDto;
        onErased: () => Promise<void> | void;
    } = $props();

    let preview = $state<ErasurePreviewDto | null>(null);
    let previewError = $state<string | null>(null);
    let requestDate = $state("");
    let confirmName = $state("");
    let busy = $state(false);

    $effect(() => {
        if (!open) return;
        const id = student.id;
        preview = null;
        previewError = null;
        requestDate = today();
        confirmName = "";
        getErasurePreview(id)
            .then((loaded) => (preview = loaded))
            .catch((e) => (previewError = errorMessage(e)));
    });

    const nameMatches = $derived(
        confirmName.trim().toLocaleLowerCase("ru") ===
            student.lastName.toLocaleLowerCase("ru"),
    );
    const canErase = $derived(
        nameMatches && !!requestDate && requestDate <= today() && !busy,
    );

    async function erase(event: SubmitEvent) {
        event.preventDefault();
        if (!canErase) return;
        busy = true;
        try {
            await eraseStudent(student.id, requestDate, confirmName.trim());
            toast.success("Данные ученика удалены", {
                description: `Отправьте заявителю уведомление об удалении с ${PRIVACY_CONTACT_EMAIL}.`,
            });
            open = false;
            await onErased();
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }
</script>

<Dialog.Root bind:open>
    <Dialog.Content class="sm:max-w-lg">
        <Dialog.Header>
            <Dialog.Title>Удалить персональные данные</Dialog.Title>
            <Dialog.Description>
                {fullName(student)}{student.grade ? `, ${student.grade}` : ""}
            </Dialog.Description>
        </Dialog.Header>

        <form id="erase-form" class="flex flex-col gap-4" onsubmit={erase}>
            <Alert.Root variant="destructive">
                <Alert.Title>Это нельзя отменить</Alert.Title>
                <Alert.Description>
                    {#if previewError}
                        Не удалось посчитать объём: {previewError}
                    {:else if !preview}
                        <Skeleton class="h-10 w-full" />
                    {:else}
                        Удалятся ФИО и класс, {countOf(preview.entries, [
                            "отметка",
                            "отметки",
                            "отметок",
                        ])}, связи с {countOf(preview.groups, [
                            "группой",
                            "группами",
                            "группами",
                        ])} и {countOf(preview.parents, [
                            "родителем",
                            "родителями",
                            "родителями",
                        ])}, запросы доступа, а также строки ученика в {countOf(
                            preview.files,
                            ["загруженном файле", "файлах", "файлах"],
                        )}. Останется только запись о факте и дате удаления.
                    {/if}
                </Alert.Description>
            </Alert.Root>

            <Field.Field>
                <Field.Label for="erase-date">Дата заявления</Field.Label>
                <Input
                    id="erase-date"
                    type="date"
                    class="w-44"
                    max={today()}
                    required
                    bind:value={requestDate}
                />
                <Field.Description>
                    Когда пришло письмо с просьбой удалить данные. Срок
                    исполнения — 14 рабочих дней.
                </Field.Description>
            </Field.Field>

            <Field.Field>
                <Field.Label for="erase-confirm">
                    Для подтверждения введите фамилию: <strong
                        >{student.lastName}</strong
                    >
                </Field.Label>
                <Input
                    id="erase-confirm"
                    autocomplete="off"
                    bind:value={confirmName}
                />
            </Field.Field>
        </form>

        <Dialog.Footer>
            <Button variant="outline" onclick={() => (open = false)}>
                Отмена
            </Button>
            <Button
                type="submit"
                form="erase-form"
                variant="destructive"
                disabled={!canErase}
            >
                {#if busy}<Spinner />{/if}
                Удалить навсегда
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
