import { getAccount } from "$lib/api/account";
import { listAccessRequests, listGroupOptions } from "$lib/api/parent";

export async function load() {
    const [groups, requests, account] = await Promise.all([
        listGroupOptions(),
        listAccessRequests(),
        // нужен только пол; без профиля просто спросим, кем приходитесь
        getAccount().catch(() => null),
    ]);
    return { groups, requests, gender: account?.gender ?? null };
}
