import type { GradeStatus } from "$lib/api/types";

export type GradeInfo = {
    status: GradeStatus;
    /** Символ из трафарета: цвет не единственный носитель смысла */
    symbol: string;
    label: string;
    /** Клавиша быстрого ввода в сетке */
    key: string;
    className: string;
};

export const GRADES: Record<GradeStatus, GradeInfo> = {
    Solved: {
        status: "Solved",
        symbol: "+",
        label: "Решил",
        key: "1",
        className: "bg-grade-solved text-white",
    },
    SolvedWithRemarks: {
        status: "SolvedWithRemarks",
        symbol: "+.",
        label: "Решил с замечаниями",
        key: "2",
        className: "bg-grade-solved-remarks text-neutral-950",
    },
    UnsolvedWithRemarks: {
        status: "UnsolvedWithRemarks",
        symbol: "-.",
        label: "Не решил, есть продвижение",
        key: "3",
        className: "bg-grade-unsolved-remarks text-neutral-950",
    },
    Unsolved: {
        status: "Unsolved",
        symbol: "-",
        label: "Не решил",
        key: "4",
        className: "bg-grade-unsolved text-white",
    },
};

/** Порядок клика по ячейке: пусто → + → +. → -. → - → пусто. */
export const GRADE_CYCLE: (GradeStatus | null)[] = [
    null,
    "Solved",
    "SolvedWithRemarks",
    "UnsolvedWithRemarks",
    "Unsolved",
];

export const GRADE_LIST: GradeInfo[] = GRADE_CYCLE.filter(
    (status): status is GradeStatus => status !== null,
).map((status) => GRADES[status]);

export function nextGrade(status: GradeStatus | null): GradeStatus | null {
    const index = GRADE_CYCLE.indexOf(status);
    return GRADE_CYCLE[(index + 1) % GRADE_CYCLE.length];
}

export function gradeByKey(key: string): GradeStatus | null | undefined {
    if (key === "0" || key === "Delete" || key === "Backspace") return null;
    return GRADE_LIST.find((grade) => grade.key === key)?.status;
}

export function isSolved(status: GradeStatus | null): boolean {
    return status === "Solved" || status === "SolvedWithRemarks";
}
