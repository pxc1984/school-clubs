<script lang="ts">
    import { page } from "$app/state";
    import { Badge } from "$lib/components/ui/badge";
    import { Button } from "$lib/components/ui/button";

    let { data, children } = $props();

    const LINKS = [
        { href: "/admin", label: "Запросы", exact: true },
        { href: "/admin/groups", label: "Группы" },
        { href: "/admin/users", label: "Пользователи" },
        { href: "/admin/students", label: "Ученики" },
    ];

    function isActive(link: (typeof LINKS)[number]): boolean {
        const path = page.url.pathname;
        return link.exact
            ? path === link.href
            : path === link.href || path.startsWith(`${link.href}/`);
    }

    function badge(href: string): number {
        if (href === "/admin") return data.pendingRequests;
        return 0;
    }
</script>

<nav
    class="-mt-2 mb-6 flex flex-wrap gap-1 border-b pb-2"
    aria-label="Администрирование"
>
    {#each LINKS as link (link.href)}
        {@const active = isActive(link)}
        {@const count = badge(link.href)}
        <Button
            href={link.href}
            size="sm"
            variant={active ? "secondary" : "ghost"}
            aria-current={active ? "page" : undefined}
        >
            {link.label}
            {#if count}
                <Badge class="h-5 min-w-5 px-1 tabular-nums">{count}</Badge>
            {/if}
        </Button>
    {/each}
</nav>

{@render children()}
