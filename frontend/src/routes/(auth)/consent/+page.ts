import { redirect } from "@sveltejs/kit";

import { session } from "$lib/session.svelte";

export async function load() {
    const me = await session.load();
    if (!me) redirect(307, "/login");
    if (!me.consentRequired) redirect(307, session.homeHref());
    return { me };
}
