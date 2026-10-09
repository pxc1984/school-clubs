<script lang="ts">
    import type { Snippet } from "svelte";
    import LogOutIcon from "@lucide/svelte/icons/log-out";
    import MoonIcon from "@lucide/svelte/icons/moon";
    import SunIcon from "@lucide/svelte/icons/sun";
    import UserIcon from "@lucide/svelte/icons/user";
    import { toggleMode } from "mode-watcher";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";
    import { avatarUrl } from "$lib/api/account";
    import { adminLogout, logout } from "$lib/api/auth";
    import { errorMessage } from "$lib/api/client";
    import * as Avatar from "$lib/components/ui/avatar";
    import { Button } from "$lib/components/ui/button";
    import * as DropdownMenu from "$lib/components/ui/dropdown-menu";
    import { fullName, initials } from "$lib/format";
    import { session } from "$lib/session.svelte";
    import { toast } from "svelte-sonner";

    let { nav }: { nav?: Snippet } = $props();

    const me = $derived(session.me);

    async function signOut() {
        try {
            if (me?.isServiceAdmin) await adminLogout();
            else await logout();
        } catch (error) {
            toast.error(errorMessage(error));
            return;
        }
        session.clear();
        await goto(me?.isServiceAdmin ? "/admin/login" : "/login");
    }
</script>

<header
    class="sticky top-0 z-40 border-b bg-background/90 backdrop-blur supports-backdrop-filter:bg-background/70"
>
    <div class="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
        <a href="/" class="shrink-0 font-semibold tracking-tight">Кружки 218</a>

        {#if session.sections.length > 1}
            <nav
                class="flex min-w-0 gap-1 overflow-x-auto"
                aria-label="Разделы"
            >
                {#each session.sections as section (section.href)}
                    {@const active = page.url.pathname.startsWith(section.href)}
                    <Button
                        href={section.href}
                        size="sm"
                        variant={active ? "secondary" : "ghost"}
                        aria-current={active ? "page" : undefined}
                    >
                        {section.label}
                    </Button>
                {/each}
            </nav>
        {/if}

        <div class="ml-auto flex items-center gap-2">
            {@render nav?.()}

            <DropdownMenu.Root>
                <DropdownMenu.Trigger>
                    {#snippet child({ props })}
                        <Button
                            {...props}
                            variant="ghost"
                            size="icon"
                            class="rounded-full"
                            aria-label="Меню пользователя"
                        >
                            <Avatar.Root class="size-8">
                                {#if me?.avatarId}
                                    <Avatar.Image
                                        src={avatarUrl(me.avatarId)}
                                        alt=""
                                    />
                                {/if}
                                <Avatar.Fallback class="text-xs">
                                    {me && !me.isServiceAdmin
                                        ? initials(me)
                                        : "A"}
                                </Avatar.Fallback>
                            </Avatar.Root>
                        </Button>
                    {/snippet}
                </DropdownMenu.Trigger>
                <DropdownMenu.Content align="end" class="w-60">
                    <DropdownMenu.Label class="font-normal">
                        <div class="truncate text-sm font-medium">
                            {me?.isServiceAdmin
                                ? "Сервисный админ"
                                : me
                                  ? fullName(me)
                                  : ""}
                        </div>
                        {#if me?.email}
                            <div class="truncate text-xs text-muted-foreground">
                                {me.email}
                            </div>
                        {/if}
                    </DropdownMenu.Label>
                    <DropdownMenu.Separator />
                    {#if !me?.isServiceAdmin}
                        <DropdownMenu.Item onSelect={() => goto("/account")}>
                            <UserIcon />
                            Профиль
                        </DropdownMenu.Item>
                    {/if}
                    <DropdownMenu.Item onSelect={toggleMode}>
                        <SunIcon class="dark:hidden" />
                        <MoonIcon class="hidden dark:block" />
                        Сменить тему
                    </DropdownMenu.Item>
                    <DropdownMenu.Separator />
                    <DropdownMenu.Item onSelect={signOut}>
                        <LogOutIcon />
                        Выйти
                    </DropdownMenu.Item>
                </DropdownMenu.Content>
            </DropdownMenu.Root>
        </div>
    </div>
</header>

<style>
    nav {
        scrollbar-width: none;
    }
</style>
