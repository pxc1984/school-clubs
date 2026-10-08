import { requireSession } from "$lib/guards";

export async function load({ url }) {
    const me = await requireSession(url);
    return { me };
}
