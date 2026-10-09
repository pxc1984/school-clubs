import { listUsers } from "$lib/api/admin";
import type { UserRole } from "$lib/api/types";

const ROLES: UserRole[] = ["Parent", "Teacher", "Admin"];

export async function load({ url }) {
    const q = url.searchParams.get("q") ?? "";
    const rawRole = url.searchParams.get("role");
    const role = ROLES.includes(rawRole as UserRole)
        ? (rawRole as UserRole)
        : null;
    const selected = url.searchParams.get("user");
    return { q, role, selected, users: await listUsers(q, role) };
}
