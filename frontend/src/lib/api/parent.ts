import { api } from "$lib/api/client";
import type {
    AccessRequestDto,
    AttendanceStatus,
    DateOnly,
    GradebookEntryTaskStatus,
    GroupRefDto,
    Guid,
    ParentRelation,
    StudentDto,
    TimeOnly,
} from "$lib/api/types";

/** Положение в группе; ниже половины API не сообщает (null). */
export type GroupPosition = "Top10" | "Top25" | "AboveHalf";

/** «Решил 4 из 5, в среднем по группе 3,2»; attended false — «не был на занятии». */
export type LastLessonDto = {
    lessonId: Guid;
    date: DateOnly;
    topic: string | null;
    attended: boolean;
    /** Отметка учителя «не был» / «опоздал»; null — её нет */
    attendance: AttendanceStatus | null;
    solved: number;
    taskCount: number;
    /** null, если посетивших (кроме ребёнка) меньше порога */
    groupAverageSolved: number | null;
};

export type ChildGroupDto = GroupRefDto & {
    lessonsAttended: number;
    lessonsTotal: number;
    lastLesson: LastLessonDto | null;
    position: GroupPosition | null;
};

export type ChildDto = StudentDto & {
    groups: ChildGroupDto[];
};

export type ChartPointDto = {
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
    lessonsAttended: number;
    lessonsTotal: number;
    tasksSolved: number;
    tasksTotal: number;
    /** Балл за период 0..1 по весам статусов; null — не был ни на одном занятии */
    score: number | null;
    position: GroupPosition | null;
    /** Сколько занятий подряд решено всё; null, если меньше двух */
    perfectStreak: number | null;
    chart: ChartPointDto[];
};

export type ChildTaskDto = {
    taskId: Guid;
    order: number;
    title: string | null;
    status: GradebookEntryTaskStatus | null;
    comment: string | null;
    /** «решили 12 из 15»; null, если посетивших меньше порога */
    groupSolved: number | null;
    groupAttended: number | null;
};

/** Занятие в ленте, новые сверху */
export type ChildLessonDto = {
    lessonId: Guid;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    attended: boolean;
    /** Отметка учителя «не был» / «опоздал»; null — её нет */
    attendance: AttendanceStatus | null;
    solved: number;
    taskCount: number;
    /** Балл за занятие 0..1; null — не был */
    score: number | null;
    groupAverageSolved: number | null;
    tasks: ChildTaskDto[];
};

export type ParentGroupDto = {
    id: Guid;
    title: string;
    description: string | null;
};

export type CreateAccessRequestRequest = {
    groupId: Guid;
    childLastName: string;
    childFirstName: string;
    childMiddleName: string | null;
    grade: string | null;
    relation: ParentRelation;
    comment: string | null;
    /** Чекбокс «Я законный представитель и согласен на обработку данных ребёнка» */
    acceptChildDataConsent: boolean;
};

/** Не схема API: query-параметры from/to сводки и ленты. */
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
): Promise<ChildLessonDto[]> {
    const response = await api.get<ChildLessonDto[]>(
        `/parent/children/${studentId}/groups/${groupId}/lessons`,
        { params: periodParams(period) },
    );
    return response.data;
}

export async function listGroupOptions(): Promise<ParentGroupDto[]> {
    const response = await api.get<ParentGroupDto[]>("/parent/groups");
    return response.data;
}

export async function listAccessRequests(): Promise<AccessRequestDto[]> {
    const response = await api.get<AccessRequestDto[]>(
        "/parent/access-requests",
    );
    return response.data;
}

export async function createAccessRequest(
    request: CreateAccessRequestRequest,
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
