import { api, apiUrl } from "$lib/api/client";
import type {
    DateOnly,
    DateTimeString,
    GradeStatus,
    Guid,
    StudentDto,
    TaskDto,
    TimeOnly,
} from "$lib/api/types";

export type LessonRefDto = {
    id: Guid;
    date: DateOnly;
    time: TimeOnly | null;
};

export type TeacherGroupDto = {
    id: Guid;
    title: string;
    description: string | null;
    studentCount: number;
    nextLesson: LessonRefDto | null;
    lastLesson: LessonRefDto | null;
    /** Прошедшие занятия без отметок или с неполными строками */
    unfilledGradebooks: number;
};

export type TeacherGroupDetailsDto = {
    id: Guid;
    title: string;
    description: string | null;
    students: StudentDto[];
};

export type LessonListItemDto = LessonRefDto & {
    topic: string | null;
    taskCount: number;
    /** Заполненность журнала 0..100 */
    filledPercent: number;
    entryCount: number;
    isPublished: boolean;
};

export type LessonDto = LessonRefDto & {
    groupId: Guid;
    groupTitle: string;
    topic: string | null;
    tasks: TaskDto[];
};

export type CreateLessonRequest = {
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    taskCount: number;
};

export type UpdateLessonRequest = {
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
};

export type GradebookEntryDto = {
    studentId: Guid;
    taskId: Guid;
    status: GradeStatus;
    comment: string | null;
};

export type ImportFileDto = {
    uploadId: Guid;
    fileName: string;
    uploadedAt: DateTimeString;
    applied: boolean;
};

export type GradebookDto = {
    lessonId: Guid;
    publishedAt: DateTimeString | null;
    students: StudentDto[];
    tasks: TaskDto[];
    entries: GradebookEntryDto[];
    imports: ImportFileDto[];
};

export type GradebookChangeDto = {
    studentId: Guid;
    taskId: Guid;
    /** null — удалить отметку */
    status: GradeStatus | null;
    comment: string | null;
};

export type GroupSummaryDto = {
    lessons: (LessonRefDto & { topic: string | null })[];
    rows: {
        student: StudentDto;
        /** Балл 0..1 по занятиям в порядке lessons; null — не был */
        scores: (number | null)[];
    }[];
};

export type ImportChangeKind = "New" | "Changed" | "Unchanged";

export type ImportChangeDto = {
    studentId: Guid;
    studentName: string;
    taskId: Guid | null;
    taskNumber: number;
    oldStatus: GradeStatus | null;
    newStatus: GradeStatus;
    kind: ImportChangeKind;
};

export type UnrecognizedRowDto = {
    row: number;
    name: string;
    /** Похожие ученики группы для ручного сопоставления */
    candidates: StudentDto[];
};

export type CellErrorDto = {
    address: string;
    value: string;
    message: string;
};

export type ImportPreviewDto = {
    uploadId: Guid;
    fileName: string;
    /** Файл от другого занятия: применение только с подтверждением */
    otherLessonDate: DateOnly | null;
    /** Нет листа _meta — сопоставление только по тексту */
    foreignFile: boolean;
    /** Для импорта с экрана группы: дата занятия из файла */
    lessonDate: DateOnly | null;
    changes: ImportChangeDto[];
    unrecognizedRows: UnrecognizedRowDto[];
    missingStudents: StudentDto[];
    cellErrors: CellErrorDto[];
    /** Номера задач из файла, которых нет в занятии */
    tasksToCreate: number[];
    warnings: string[];
};

export type ApplyImportRequest = {
    /** Ручные сопоставления нераспознанных строк; studentId null — пропустить */
    mappings: { row: number; studentId: Guid | null }[];
    confirmOtherLesson: boolean;
    createTasks: boolean;
    publish: boolean;
};

export type ApplyImportResponse = {
    lessonId: Guid;
    applied: number;
};

export type AddStudentRequest = {
    firstName: string;
    middleName: string | null;
    lastName: string;
    grade: string | null;
};

export async function listTeacherGroups(): Promise<TeacherGroupDto[]> {
    const response = await api.get<TeacherGroupDto[]>("/teacher/groups");
    return response.data;
}

export async function getTeacherGroup(
    id: Guid,
): Promise<TeacherGroupDetailsDto> {
    const response = await api.get<TeacherGroupDetailsDto>(
        `/teacher/groups/${id}`,
    );
    return response.data;
}

export async function addGroupStudent(
    groupId: Guid,
    request: AddStudentRequest,
): Promise<StudentDto> {
    const response = await api.post<StudentDto>(
        `/teacher/groups/${groupId}/students`,
        request,
    );
    return response.data;
}

export async function removeGroupStudent(
    groupId: Guid,
    studentId: Guid,
): Promise<void> {
    await api.delete(`/teacher/groups/${groupId}/students/${studentId}`);
}

export async function listLessons(groupId: Guid): Promise<LessonListItemDto[]> {
    const response = await api.get<LessonListItemDto[]>(
        `/teacher/groups/${groupId}/lessons`,
    );
    return response.data;
}

export async function createLesson(
    groupId: Guid,
    request: CreateLessonRequest,
): Promise<LessonDto> {
    const response = await api.post<LessonDto>(
        `/teacher/groups/${groupId}/lessons`,
        request,
    );
    return response.data;
}

export async function getGroupSummary(groupId: Guid): Promise<GroupSummaryDto> {
    const response = await api.get<GroupSummaryDto>(
        `/teacher/groups/${groupId}/summary`,
    );
    return response.data;
}

export async function getLesson(id: Guid): Promise<LessonDto> {
    const response = await api.get<LessonDto>(`/teacher/lessons/${id}`);
    return response.data;
}

export async function updateLesson(
    id: Guid,
    request: UpdateLessonRequest,
): Promise<LessonDto> {
    const response = await api.put<LessonDto>(
        `/teacher/lessons/${id}`,
        request,
    );
    return response.data;
}

export async function deleteLesson(id: Guid): Promise<void> {
    await api.delete(`/teacher/lessons/${id}`);
}

export async function addTask(
    lessonId: Guid,
    title: string | null,
): Promise<TaskDto> {
    const response = await api.post<TaskDto>(
        `/teacher/lessons/${lessonId}/tasks`,
        { title },
    );
    return response.data;
}

export async function renameTask(
    lessonId: Guid,
    taskId: Guid,
    title: string | null,
): Promise<TaskDto> {
    const response = await api.put<TaskDto>(
        `/teacher/lessons/${lessonId}/tasks/${taskId}`,
        { title },
    );
    return response.data;
}

export async function deleteTask(lessonId: Guid, taskId: Guid): Promise<void> {
    await api.delete(`/teacher/lessons/${lessonId}/tasks/${taskId}`);
}

export async function getGradebook(lessonId: Guid): Promise<GradebookDto> {
    const response = await api.get<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook`,
    );
    return response.data;
}

export async function saveGradebook(
    lessonId: Guid,
    changes: GradebookChangeDto[],
): Promise<void> {
    await api.put(`/teacher/lessons/${lessonId}/gradebook`, {
        entries: changes,
    });
}

export async function publishGradebook(lessonId: Guid): Promise<void> {
    await api.post(`/teacher/lessons/${lessonId}/gradebook/publish`);
}

export async function unpublishGradebook(lessonId: Guid): Promise<void> {
    await api.post(`/teacher/lessons/${lessonId}/gradebook/unpublish`);
}

export function lessonTemplateUrl(lessonId: Guid): string {
    return apiUrl(`/teacher/lessons/${lessonId}/gradebook/template`);
}

export function groupTemplateUrl(groupId: Guid, tasks: number): string {
    return apiUrl(
        `/teacher/groups/${groupId}/gradebook-template?tasks=${tasks}`,
    );
}

export function importFileUrl(lessonId: Guid, uploadId: Guid): string {
    return apiUrl(
        `/teacher/lessons/${lessonId}/gradebook/import/${uploadId}/file`,
    );
}

/** Куда грузить XLSX: в конкретное занятие или в группу (занятие по дате из файла). */
export type ImportTarget =
    { kind: "lesson"; lessonId: Guid } | { kind: "group"; groupId: Guid };

function importBase(target: ImportTarget): string {
    return target.kind === "lesson"
        ? `/teacher/lessons/${target.lessonId}/gradebook/import`
        : `/teacher/groups/${target.groupId}/gradebook/import`;
}

export async function previewImport(
    target: ImportTarget,
    file: File,
): Promise<ImportPreviewDto> {
    const form = new FormData();
    form.append("file", file);
    const response = await api.post<ImportPreviewDto>(
        importBase(target),
        form,
        { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
}

export async function applyImport(
    target: ImportTarget,
    uploadId: Guid,
    request: ApplyImportRequest,
): Promise<ApplyImportResponse> {
    const response = await api.post<ApplyImportResponse>(
        `${importBase(target)}/${uploadId}/apply`,
        request,
    );
    return response.data;
}
