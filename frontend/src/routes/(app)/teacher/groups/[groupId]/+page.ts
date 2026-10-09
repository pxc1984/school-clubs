import { getGroupSummary, getTeacherGroup } from "$lib/api/teacher";

export type GroupTab = "lessons" | "students" | "summary";

const TABS: GroupTab[] = ["lessons", "students", "summary"];

export async function load({ params, url }) {
    const raw = url.searchParams.get("tab");
    const tab: GroupTab = TABS.includes(raw as GroupTab)
        ? (raw as GroupTab)
        : "lessons";

    const [group, summary] = await Promise.all([
        getTeacherGroup(params.groupId),
        tab === "summary" ? getGroupSummary(params.groupId) : null,
    ]);
    // свежие занятия сверху
    const lessons = group.lessons.toSorted((a, b) =>
        b.date === a.date
            ? (b.time ?? "").localeCompare(a.time ?? "")
            : b.date.localeCompare(a.date),
    );
    return { tab, group, lessons, summary };
}
