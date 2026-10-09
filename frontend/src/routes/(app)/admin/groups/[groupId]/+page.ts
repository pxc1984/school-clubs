import { getAdminGroup } from "$lib/api/admin";

export async function load({ params }) {
    return { group: await getAdminGroup(params.groupId) };
}
