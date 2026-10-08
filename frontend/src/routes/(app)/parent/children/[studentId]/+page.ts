import { error, redirect } from "@sveltejs/kit";

import {
    getChildSummary,
    listChildLessons,
    listChildren,
} from "$lib/api/parent";
import { parsePeriodKey, periodRange } from "$lib/stats";

export async function load({ params, url }) {
    const children = await listChildren();
    const child = children.find((c) => c.id === params.studentId);
    if (!child) error(404, "Ребёнок не найден");

    const groupId = url.searchParams.get("group");
    const group = child.groups.find((g) => g.id === groupId);
    if (!group) {
        if (!child.groups.length) return { child, group: null };
        redirect(
            307,
            `/parent/children/${child.id}?group=${child.groups[0].id}`,
        );
    }

    const periodKey = parsePeriodKey(url.searchParams.get("period"));
    const period = periodRange(periodKey);
    const [summary, lessons] = await Promise.all([
        getChildSummary(child.id, group.id, period),
        listChildLessons(child.id, group.id, period),
    ]);

    return { child, group, periodKey, summary, lessons };
}
