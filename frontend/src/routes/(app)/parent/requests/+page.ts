import { listAccessRequests, listGroupOptions } from "$lib/api/parent";

export async function load() {
    const [groups, requests] = await Promise.all([
        listGroupOptions(),
        listAccessRequests(),
    ]);
    return { groups, requests };
}
