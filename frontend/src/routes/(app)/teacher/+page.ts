import { listTeacherGroups } from "$lib/api/teacher";

export async function load() {
    return { groups: await listTeacherGroups() };
}
