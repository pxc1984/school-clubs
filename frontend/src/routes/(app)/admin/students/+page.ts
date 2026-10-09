import { listPendingErasures, listStudents } from "$lib/api/admin";

export async function load({ url }) {
    const q = url.searchParams.get("q") ?? "";
    const selected = url.searchParams.get("student");
    const [students, erasures] = await Promise.all([
        listStudents(q),
        // недочищенные удаления — подсказка, без неё страница работает
        listPendingErasures().catch(() => []),
    ]);
    return { q, selected, students, erasures };
}
