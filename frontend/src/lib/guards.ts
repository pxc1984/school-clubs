import { redirect } from "@sveltejs/kit";

import type { MeDto } from "$lib/api/auth";
import type { UserRole } from "$lib/api/types";
import { session } from "$lib/session.svelte";

// Гварды — только UX: права всё равно проверяет сервер.

function returnUrlOf(url: URL): string {
    return encodeURIComponent(url.pathname + url.search);
}

export async function requireSession(url: URL): Promise<MeDto> {
    const me = await session.load();
    if (!me) {
        const login = url.pathname.startsWith("/admin")
            ? "/admin/login"
            : "/login";
        redirect(307, `${login}?returnUrl=${returnUrlOf(url)}`);
    }
    if (me.consentRequired)
        redirect(307, `/consent?returnUrl=${returnUrlOf(url)}`);
    return me;
}

export function requireRole(me: MeDto, role: UserRole): MeDto {
    if (!me.roles.includes(role)) redirect(307, "/");
    return me;
}
