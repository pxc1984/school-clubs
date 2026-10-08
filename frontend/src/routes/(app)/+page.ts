import { redirect } from "@sveltejs/kit";

import { session } from "$lib/session.svelte";

export async function load({ parent }) {
    await parent();
    redirect(307, session.homeHref());
}
