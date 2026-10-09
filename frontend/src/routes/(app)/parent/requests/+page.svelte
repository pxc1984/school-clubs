<script lang="ts">
    import { toast } from "svelte-sonner";
    import { invalidateAll } from "$app/navigation";
    import { errorMessage, fieldErrors } from "$lib/api/client";
    import { cancelAccessRequest, createAccessRequest } from "$lib/api/parent";
    import type { AccessRequestDto, ParentRelation } from "$lib/api/types";
    import ConfirmDialog from "$lib/components/confirm-dialog.svelte";
    import ConsentCheckbox from "$lib/components/consent-checkbox.svelte";
    import PageHeader from "$lib/components/page-header.svelte";
    import RequestStatusBadge from "$lib/components/request-status-badge.svelte";
    import * as Alert from "$lib/components/ui/alert";
    import { Button } from "$lib/components/ui/button";
    import * as Field from "$lib/components/ui/field";
    import { Input } from "$lib/components/ui/input";
    import * as NativeSelect from "$lib/components/ui/native-select";
    import * as RadioGroup from "$lib/components/ui/radio-group";
    import { Separator } from "$lib/components/ui/separator";
    import { Spinner } from "$lib/components/ui/spinner";
    import { Textarea } from "$lib/components/ui/textarea";
    import { formatDateTime } from "$lib/format";
    import { CHILD_CONSENT_PREFIX } from "$lib/legal";

    const MAX_PENDING = 5;

    const RELATIONS: { value: ParentRelation; label: string }[] = [
        { value: "Mother", label: "Мама" },
        { value: "Father", label: "Папа" },
        { value: "Guardian", label: "Опекун" },
        { value: "Other", label: "Другое" },
    ];

    let { data } = $props();

    // по полу из профиля понятно, мама или папа, — спрашиваем, только если это не так
    const relationFromGender = $derived<ParentRelation | null>(
        data.gender === "Female"
            ? "Mother"
            : data.gender === "Male"
              ? "Father"
              : null,
    );
    let relationOverridden = $state(false);
    const askRelation = $derived(!relationFromGender || relationOverridden);

    const pendingCount = $derived(
        data.requests.filter((r) => r.status === "Pending").length,
    );
    const limitReached = $derived(pendingCount >= MAX_PENDING);

    function emptyForm() {
        return {
            groupId: "",
            lastName: "",
            firstName: "",
            middleName: "",
            grade: "",
            relation: "" as ParentRelation | "",
            comment: "",
            consent: false,
        };
    }

    let form = $state(emptyForm());
    let errors = $state<Record<string, string>>({});
    let busy = $state(false);

    const selectedGroup = $derived(
        data.groups.find((g) => g.id === form.groupId),
    );

    function validate(): Record<string, string> {
        const result: Record<string, string> = {};
        if (!form.groupId) result.groupId = "Выберите кружок";
        if (!form.lastName.trim()) result.childLastName = "Укажите фамилию";
        if (!form.firstName.trim()) result.childFirstName = "Укажите имя";
        if (askRelation && !form.relation)
            result.relation = "Укажите, кем приходитесь";
        if (!form.consent)
            result.acceptChildDataConsent = "Без согласия запрос не отправить";
        return result;
    }

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        errors = validate();
        if (Object.keys(errors).length) return;

        busy = true;
        try {
            await createAccessRequest({
                groupId: form.groupId,
                childLastName: form.lastName.trim(),
                childFirstName: form.firstName.trim(),
                childMiddleName: form.middleName.trim() || null,
                grade: form.grade.trim() || null,
                relation: askRelation
                    ? (form.relation as ParentRelation)
                    : null,
                comment: form.comment.trim() || null,
                acceptChildDataConsent: form.consent,
            });
            form = emptyForm();
            relationOverridden = false;
            await invalidateAll();
            toast.success("Запрос отправлен", {
                description: "Администратор проверит его и откроет доступ.",
            });
        } catch (e) {
            errors = fieldErrors(e);
            if (!Object.keys(errors).length) toast.error(errorMessage(e));
        } finally {
            busy = false;
        }
    }

    let cancelling = $state<AccessRequestDto | null>(null);
    let cancelOpen = $state(false);
</script>

<svelte:head><title>Запрос доступа — Кружки 218</title></svelte:head>

<div class="mx-auto max-w-2xl">
    <PageHeader
        title="Запрос доступа"
        description="Укажите кружок и данные ребёнка — администратор сверит их со списком группы и откроет доступ к отметкам."
        back={{ href: "/parent", label: "Мои дети" }}
    />

    {#if limitReached}
        <Alert.Root class="mb-6">
            <Alert.Description>
                У вас {MAX_PENDING} запросов на рассмотрении — это максимум. Дождитесь
                решения или отзовите один из них.
            </Alert.Description>
        </Alert.Root>
    {/if}

    <form onsubmit={submit} class="flex flex-col gap-6" novalidate>
        <fieldset disabled={limitReached || busy} class="contents">
            <Field.Group>
                <Field.Field data-invalid={!!errors.groupId}>
                    <Field.Label for="group">Кружок</Field.Label>
                    <NativeSelect.Root
                        id="group"
                        class="w-full"
                        bind:value={form.groupId}
                        aria-invalid={!!errors.groupId}
                    >
                        <NativeSelect.Option value="" disabled>
                            Выберите кружок
                        </NativeSelect.Option>
                        {#each data.groups as group (group.id)}
                            <NativeSelect.Option value={group.id}>
                                {group.title}
                            </NativeSelect.Option>
                        {/each}
                    </NativeSelect.Root>
                    {#if selectedGroup?.description}
                        <Field.Description>
                            {selectedGroup.description}
                        </Field.Description>
                    {/if}
                    <Field.Error>{errors.groupId}</Field.Error>
                </Field.Field>

                <Field.Set>
                    <Field.Legend>Ребёнок</Field.Legend>
                    <Field.Group class="grid gap-4 sm:grid-cols-2">
                        <Field.Field data-invalid={!!errors.childLastName}>
                            <Field.Label for="lastName">Фамилия</Field.Label>
                            <Input id="lastName" bind:value={form.lastName} />
                            <Field.Error>{errors.childLastName}</Field.Error>
                        </Field.Field>
                        <Field.Field data-invalid={!!errors.childFirstName}>
                            <Field.Label for="firstName">Имя</Field.Label>
                            <Input id="firstName" bind:value={form.firstName} />
                            <Field.Error>{errors.childFirstName}</Field.Error>
                        </Field.Field>
                        <Field.Field>
                            <Field.Label for="middleName">Отчество</Field.Label>
                            <Input
                                id="middleName"
                                bind:value={form.middleName}
                            />
                        </Field.Field>
                        <Field.Field data-invalid={!!errors.grade}>
                            <Field.Label for="grade">Класс</Field.Label>
                            <Input
                                id="grade"
                                placeholder="7Б"
                                maxlength={8}
                                bind:value={form.grade}
                            />
                            <Field.Error>{errors.grade}</Field.Error>
                        </Field.Field>
                    </Field.Group>
                </Field.Set>

                {#if !askRelation}
                    <Field.Field>
                        <Field.Title>Кем приходитесь</Field.Title>
                        <Field.Description>
                            {relationFromGender === "Mother" ? "Мама" : "Папа"} —
                            по полу в
                            <a href="/account" class="underline">профиле</a>.
                            <Button
                                variant="link"
                                size="xs"
                                class="h-auto p-0"
                                onclick={() => (relationOverridden = true)}
                            >
                                Я опекун или другое
                            </Button>
                        </Field.Description>
                    </Field.Field>
                {:else}
                    <Field.Set data-invalid={!!errors.relation}>
                        <Field.Legend variant="label"
                            >Кем приходитесь</Field.Legend
                        >
                        <RadioGroup.Root
                            bind:value={form.relation}
                            class="flex flex-wrap gap-4"
                        >
                            {#each RELATIONS as relation (relation.value)}
                                <Field.Field
                                    orientation="horizontal"
                                    class="w-auto"
                                >
                                    <RadioGroup.Item
                                        value={relation.value}
                                        id="relation-{relation.value}"
                                    />
                                    <Field.Label
                                        for="relation-{relation.value}"
                                        class="font-normal"
                                    >
                                        {relation.label}
                                    </Field.Label>
                                </Field.Field>
                            {/each}
                        </RadioGroup.Root>
                        <Field.Error>{errors.relation}</Field.Error>
                    </Field.Set>
                {/if}

                <Field.Field>
                    <Field.Label for="comment">Комментарий</Field.Label>
                    <Textarea
                        id="comment"
                        rows={3}
                        placeholder="Например: в группе есть тёзка, у нас день занятий — среда"
                        bind:value={form.comment}
                    />
                </Field.Field>

                <ConsentCheckbox
                    id="consent"
                    bind:checked={form.consent}
                    prefix={CHILD_CONSENT_PREFIX}
                    error={errors.acceptChildDataConsent}
                />
            </Field.Group>

            <div>
                <Button type="submit">
                    {#if busy}<Spinner />{/if}
                    Отправить запрос
                </Button>
            </div>
        </fieldset>
    </form>

    {#if data.requests.length}
        <Separator class="my-8" />
        <section>
            <h2 class="mb-3 text-lg font-semibold">Мои запросы</h2>
            <ul class="divide-y rounded-lg border">
                {#each data.requests as request (request.id)}
                    <li class="flex flex-col gap-1 p-4">
                        <div class="flex flex-wrap items-center gap-2">
                            <span class="font-medium">
                                {request.childLastName}
                                {request.childFirstName}
                                {request.childMiddleName ?? ""}
                            </span>
                            <RequestStatusBadge status={request.status} />
                            {#if request.status === "Pending"}
                                <Button
                                    variant="ghost"
                                    size="xs"
                                    class="ml-auto"
                                    onclick={() => {
                                        cancelling = request;
                                        cancelOpen = true;
                                    }}
                                >
                                    Отозвать
                                </Button>
                            {/if}
                        </div>
                        <div class="text-sm text-muted-foreground">
                            {request.group.title}{request.grade
                                ? ` · ${request.grade}`
                                : ""} · отправлен {formatDateTime(
                                request.created,
                            )}
                        </div>
                        {#if request.status === "Rejected" && request.rejectReason}
                            <p class="mt-1 text-sm">
                                <span class="text-muted-foreground"
                                    >Причина отказа:</span
                                >
                                {request.rejectReason}
                            </p>
                        {/if}
                    </li>
                {/each}
            </ul>
        </section>
    {/if}
</div>

<ConfirmDialog
    bind:open={cancelOpen}
    title="Отозвать запрос?"
    description="Запрос будет закрыт. При необходимости можно отправить новый."
    confirmText="Отозвать"
    destructive
    onConfirm={async () => {
        if (!cancelling) return;
        await cancelAccessRequest(cancelling.id);
        await invalidateAll();
        toast.success("Запрос отозван");
    }}
/>
