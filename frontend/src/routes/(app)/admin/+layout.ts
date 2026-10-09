import { getCounters } from "$lib/api/admin";
import { requireRole } from "$lib/guards";

export async function load({ parent, depends }) {
    const { me } = await parent();
    requireRole(me, "Admin");
    depends("admin:counters");
    // счётчики — подсказка в навигации, без них раздел работает
    const counters = await getCounters().catch(() => null);
    return { counters };
}
