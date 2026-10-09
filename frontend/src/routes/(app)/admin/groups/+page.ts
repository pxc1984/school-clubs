import { listAdminGroups } from "$lib/api/admin";

export async function load({ url }) {
    const archived = url.searchParams.get("archived") === "1";
    return { archived, groups: await listAdminGroups(archived) };
}
