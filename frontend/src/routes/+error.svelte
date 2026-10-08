<script lang="ts">
    import CircleAlertIcon from "@lucide/svelte/icons/circle-alert";
    import { page } from "$app/state";
    import { Button } from "$lib/components/ui/button";
    import * as Empty from "$lib/components/ui/empty";

    const title = $derived(
        page.status === 404
            ? "Страница не найдена"
            : page.status === 403
              ? "Нет доступа"
              : "Не удалось загрузить страницу",
    );
</script>

<div class="flex min-h-screen items-center justify-center p-4">
    <Empty.Root>
        <Empty.Header>
            <Empty.Media variant="icon">
                <CircleAlertIcon />
            </Empty.Media>
            <Empty.Title>{title}</Empty.Title>
            <Empty.Description>
                {page.status === 404
                    ? "Возможно, ссылка устарела."
                    : (page.error?.message ?? "Попробуйте обновить страницу.")}
            </Empty.Description>
        </Empty.Header>
        <Empty.Content>
            <div class="flex gap-2">
                <Button variant="outline" onclick={() => location.reload()}>
                    Обновить
                </Button>
                <Button href="/">На главную</Button>
            </div>
        </Empty.Content>
    </Empty.Root>
</div>
