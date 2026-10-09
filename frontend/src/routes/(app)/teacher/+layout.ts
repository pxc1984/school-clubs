import { requireRole } from "$lib/guards";

export async function load({ parent }) {
    const { me } = await parent();
    requireRole(me, "Teacher");
}
