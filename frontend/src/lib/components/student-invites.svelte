<script lang="ts">
    import { toast } from "svelte-sonner";
    import CopyIcon from "@lucide/svelte/icons/copy";
    import LinkIcon from "@lucide/svelte/icons/link";
    import XIcon from "@lucide/svelte/icons/x";
    import {
        createStudentInvite,
        inviteUrl,
        listStudentInvites,
        revokeStudentInvite,
        type ChildInviteDto,
    } from "$lib/api/admin";
    import { errorMessage } from "$lib/api/client";
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import { Spinner } from "$lib/components/ui/spinner";
    import { formatDateTime } from "$lib/format";

    let { studentId }: { studentId: string } = $props();

    let invites = $state<ChildInviteDto[]>([]);
    let creating = $state(false);

    $effect(() => {
        const id = studentId;
        invites = [];
        listStudentInvites(id)
            .then((loaded) => {
                if (studentId === id) invites = loaded;
            })
            .catch((e) => toast.error(errorMessage(e)));
    });

    async function copy(invite: ChildInviteDto) {
        try {
            await navigator.clipboard.writeText(inviteUrl(invite));
            toast.success("Ссылка скопирована", {
                description: "Отправьте её родителю.",
            });
        } catch {
            toast.error("Не удалось скопировать — выделите ссылку вручную");
        }
    }

    async function create() {
        creating = true;
        try {
            const invite = await createStudentInvite(studentId);
            invites = [invite, ...invites];
            await copy(invite);
        } catch (e) {
            toast.error(errorMessage(e));
        } finally {
            creating = false;
        }
    }

    async function revoke(invite: ChildInviteDto) {
        try {
            await revokeStudentInvite(studentId, invite.id);
            invites = invites.filter((i) => i.id !== invite.id);
            toast.success("Ссылка отозвана");
        } catch (e) {
            toast.error(errorMessage(e));
        }
    }
</script>

<section class="flex flex-col gap-2">
    <h3 class="font-medium">Ссылка для родителя</h3>
    <p class="text-muted-foreground">
        Родитель перейдёт по ссылке, войдёт или зарегистрируется — и ребёнок
        сразу появится в его кабинете. Ссылка одноразовая.
    </p>

    {#each invites as invite (invite.id)}
        <div class="flex items-center gap-1.5">
            <Input
                readonly
                value={inviteUrl(invite)}
                aria-label="Ссылка-приглашение"
                title="Действует до {formatDateTime(invite.expiresAt)}"
                class="h-8 min-w-0 flex-1 font-mono text-xs"
                onfocus={(e) => e.currentTarget.select()}
            />
            <Button
                variant="outline"
                size="icon-sm"
                aria-label="Скопировать ссылку"
                title="Скопировать"
                onclick={() => copy(invite)}
            >
                <CopyIcon />
            </Button>
            <Button
                variant="ghost"
                size="icon-sm"
                aria-label="Отозвать ссылку"
                title="Отозвать"
                onclick={() => revoke(invite)}
            >
                <XIcon />
            </Button>
        </div>
    {/each}

    <Button
        variant="outline"
        size="sm"
        class="w-fit"
        disabled={creating}
        onclick={create}
    >
        {#if creating}<Spinner />{:else}<LinkIcon />{/if}
        Создать и скопировать ссылку
    </Button>
</section>
