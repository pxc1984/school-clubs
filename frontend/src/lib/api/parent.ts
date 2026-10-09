import { api } from "$lib/api/client";
import type {
    AccessRequestDto,
    DateOnly,
    GradeStatus,
    GroupRefDto,
    GroupShortDto,
    Guid,
    Relation,
    StudentDto,
    TimeOnly,
} from "$lib/api/types";

/** Корзины положения в группе; ниже половины API не сообщает. */
export type PercentileBucket = "Top10" | "Top25" | "AboveHalf";

export type LessonResultDto = {
    lessonId: Guid;
    date: DateOnly;
    topic: string | null;
    /** false — «не был на занятии» */
    attended: boolean;
    solved: number;
    taskCount: number;
    /** null, если посетивших (кроме ребёнка) меньше порога */
    groupAverageSolved: number | null;
};

export type ChildGroupDto = GroupRefDto & {
    lessonsAttended: number;
    lessonsTotal: number;
    lastLesson: LessonResultDto | null;
    position: PercentileBucket | null;
};

export type ChildDto = StudentDto & {
    groups: ChildGroupDto[];
};

export type SeriesPointDto = {
    lessonId: Guid;
    date: DateOnly;
    /** Балл ребёнка 0..1; null — не был на занятии */
    score: number | null;
    groupMedian: number | null;
    /** Полоса 25–75 перцентиль; null, если учеников меньше порога */
    groupP25: number | null;
    groupP75: number | null;
};

export type ChildSummaryDto = {
    student: StudentDto;
    group: GroupRefDto;
    from: DateOnly | null;
    to: DateOnly | null;
    /** Балл за период 0..1 по весам статусов */
    score: number | null;
    tasksSolved: number;
    tasksTotal: number;
    lessonsAttended: number;
    lessonsTotal: number;
    position: PercentileBucket | null;
    /** Сколько занятий подряд решено всё; null, если меньше двух */
    perfectStreak: number | null;
    chart: SeriesPointDto[];
};

export type ParentTaskDto = {
    taskId: Guid;
    order: number;
    title: string | null;
    status: GradeStatus | null;
    comment: string | null;
    /** «решили 12 из 15»; null, если посетивших меньше порога */
    groupSolved: number | null;
    groupAttended: number | null;
};

export type ParentLessonDto = {
    lessonId: Guid;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    attended: boolean;
    solved: number;
    taskCount: number;
    /** Балл за занятие 0..1; null — не был */
    score: number | null;
    groupAverageSolved: number | null;
    tasks: ParentTaskDto[];
};

export type GroupOptionDto = GroupShortDto & {
    description: string | null;
};

export type CreateAccessRequest = {
    groupId: Guid;
    childFirstName: string;
    childMiddleName: string | null;
    childLastName: string;
    grade: string | null;
    relation: Relation;
    comment: string | null;
    /** Чекбокс «Я законный представитель и согласен на обработку данных ребёнка» */
    acceptChildDataConsent: boolean;
};

export type Period = { from: DateOnly | null; to: DateOnly | null };

function periodParams(period: Period) {
    return {
        from: period.from ?? undefined,
        to: period.to ?? undefined,
    };
}

export async function listChildren(): Promise<ChildDto[]> {
    const response = await api.get<ChildDto[]>("/parent/children");
    return response.data;
}

export async function getChildSummary(
    studentId: Guid,
    groupId: Guid,
    period: Period,
): Promise<ChildSummaryDto> {
    const response = await api.get<ChildSummaryDto>(
        `/parent/children/${studentId}/groups/${groupId}/summary`,
        { params: periodParams(period) },
    );
    return response.data;
}

export async function listChildLessons(
    studentId: Guid,
    groupId: Guid,
    period: Period,
): Promise<ParentLessonDto[]> {
    const response = await api.get<ParentLessonDto[]>(
        `/parent/children/${studentId}/groups/${groupId}/lessons`,
        { params: periodParams(period) },
    );
    return response.data;
}

export async function listGroupOptions(): Promise<GroupOptionDto[]> {
    const response = await api.get<GroupOptionDto[]>("/parent/groups");
    return response.data;
}

export async function listAccessRequests(): Promise<AccessRequestDto[]> {
    const response = await api.get<AccessRequestDto[]>(
        "/parent/access-requests",
    );
    return response.data;
}

export async function createAccessRequest(
    request: CreateAccessRequest,
): Promise<AccessRequestDto> {
    const response = await api.post<AccessRequestDto>(
        "/parent/access-requests",
        request,
    );
    return response.data;
}

export async function cancelAccessRequest(id: Guid): Promise<void> {
    await api.delete(`/parent/access-requests/${id}`);
}
