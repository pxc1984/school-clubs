import type { GroupPosition, Period } from "$lib/api/parent";
import { toDateOnly } from "$lib/format";

export const POSITION_LABELS: Record<GroupPosition, string> = {
    Top10: "В топ-10% группы",
    Top25: "В топ-25% группы",
    AboveHalf: "Выше половины группы",
};

export type PeriodKey = "month" | "quarter" | "all";

export const PERIODS: { key: PeriodKey; label: string }[] = [
    { key: "month", label: "Месяц" },
    { key: "quarter", label: "Четверть" },
    { key: "all", label: "Всё время" },
];

export function parsePeriodKey(value: string | null): PeriodKey {
    return value === "month" || value === "all" ? value : "quarter";
}

/** Скользящее окно до сегодняшнего дня: месяц — 1, четверть — 3 месяца. */
export function periodRange(key: PeriodKey, now = new Date()): Period {
    if (key === "all") return { from: null, to: null };
    const from = new Date(now);
    from.setMonth(from.getMonth() - (key === "month" ? 1 : 3));
    return { from: toDateOnly(from), to: toDateOnly(now) };
}
