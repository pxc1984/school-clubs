import { api } from "$lib/api/client";
import type {
    AccessRequestDto,
    DateOnly,
    GradeStatus,
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
    solved: number;
    total: number;
    /** null, если посетивших (кроме ребёнка) меньше порога */
    groupAverageSolved: number | null;
};

export type ChildGroupDto = GroupShortDto & {
    lastLesson: LessonResultDto | null;
    bucket: PercentileBucket | null;
};

export type ChildDto = StudentDto & {
    groups: ChildGroupDto[];
};

export type SeriesPointDto = {
    lessonId: Guid;
    date: DateOnly;
    /** Балл ребёнка 0..1; null — не был на занятии */
    score: number | null;
    median: number | null;
    /** Полоса 25–75 перцентиль; null, если учеников меньше порога */
    p25: number | null;
    p75: number | null;
};

export type ChildSummaryDto = {
    /** Балл за период 0..1 по весам статусов */
    score: number | null;
    solvedTasks: number;
    totalTasks: number;
    attendedLessons: number;
    totalLessons: number;
    bucket: PercentileBucket | null;
    /** Сколько занятий подряд решено всё; null, если меньше двух */
    streak: number | null;
    series: SeriesPointDto[];
};

export type ParentTaskDto = {
    taskId: Guid;
    order: number;
    title: string | null;
    status: GradeStatus | null;
    comment: string | null;
    /** «решили 12 из 15»; null, если посетивших меньше порога */
    solvedCount: number | null;
    attendedCount: number | null;
};

export type ParentLessonDto = {
    lessonId: Guid;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    attended: boolean;
    solved: number;
    total: number;
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
    acceptPersonalDataPolicy: boolean;
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
