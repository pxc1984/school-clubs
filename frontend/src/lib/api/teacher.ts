import { api, apiUrl } from "$lib/api/client";
import type {
    AttendanceStatus,
    DateOnly,
    DateTimeString,
    GradebookEntryTaskStatus,
    Guid,
    NewStudentRequest,
    StudentDto,
    TaskDto,
    TimeOnly,
} from "$lib/api/types";

// --- группы

export type TeacherGroupCardDto = {
    id: Guid;
    title: string;
    description: string | null;
    studentCount: number;
    lastLessonDate: DateOnly | null;
    nextLessonDate: DateOnly | null;
    /** Прошедшие занятия (включая сегодня) с неполностью заполненным журналом */
    unfilledLessonCount: number;
};

export type LessonSummaryDto = {
    id: Guid;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    taskCount: number;
    /** Доля заполненных ячеек для текущего состава, 0–100 */
    filledPercent: number;
    isPublished: boolean;
};

export type TeacherGroupDto = {
    id: Guid;
    title: string;
    description: string | null;
    isArchived: boolean;
    students: StudentDto[];
    lessons: LessonSummaryDto[];
};

export type SummaryLessonDto = {
    id: Guid;
    date: DateOnly;
    topic: string | null;
    taskCount: number;
    isPublished: boolean;
};

/** inGroup false — выбывший ученик, у которого остались отметки */
export type SummaryStudentDto = StudentDto & { inGroup: boolean };

/** Ячейка есть, только если у ученика есть отметки или посещаемость на занятии */
export type SummaryCellDto = {
    studentId: Guid;
    lessonId: Guid;
    marked: number;
    /** Решено, в том числе с замечаниями */
    solved: number;
    /** «Не был» / «опоздал»; null — отметки посещаемости нет */
    attendance: AttendanceStatus | null;
};

export type GroupMatrixDto = {
    lessons: SummaryLessonDto[];
    students: SummaryStudentDto[];
    cells: SummaryCellDto[];
};

// --- занятия

export type LessonDto = {
    id: Guid;
    groupId: Guid;
    groupTitle: string;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    tasks: TaskDto[];
    publishedAt: DateTimeString | null;
};

export type CreateLessonRequest = {
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    /** null — как в последнем занятии группы */
    taskCount: number | null;
};

export type UpdateLessonRequest = {
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
};

export type TaskTitleRequest = {
    title: string | null;
};

// --- журнал

/** inGroup false — выбывший ученик, у которого остались отметки */
export type GradebookStudentDto = StudentDto & { inGroup: boolean };

export type GradebookCellDto = {
    studentId: Guid;
    taskId: Guid;
    status: GradebookEntryTaskStatus;
    comment: string | null;
};

/** Отметка посещаемости; ученики без неё были вовремя */
export type GradebookAttendanceDto = {
    studentId: Guid;
    status: AttendanceStatus;
};

export type GradebookDto = {
    lessonId: Guid;
    date: DateOnly;
    topic: string | null;
    publishedAt: DateTimeString | null;
    tasks: TaskDto[];
    students: GradebookStudentDto[];
    entries: GradebookCellDto[];
    attendance: GradebookAttendanceDto[];
};

export type GradebookCellChange = {
    studentId: Guid;
    taskId: Guid;
    /** null — очистить ячейку */
    status: GradebookEntryTaskStatus | null;
    /** Ячейка задаётся целиком: null убирает комментарий */
    comment: string | null;
};

export type GradebookAttendanceChange = {
    studentId: Guid;
    /** null — был вовремя (снять отметку) */
    status: AttendanceStatus | null;
};

export type SaveGradebookRequest = {
    changes: GradebookCellChange[];
    /** null — посещаемость без изменений */
    attendance?: GradebookAttendanceChange[] | null;
};

// --- импорт XLSX

export type ImportFileDto = {
    uploadId: Guid;
    fileName: string;
    sizeBytes: number;
    uploadedAt: DateTimeString;
    /** Этот файл сейчас прикреплён к журналу (последний применённый импорт) */
    attached: boolean;
};

export type ImportChangeKind = "New" | "Changed" | "Unchanged";

export type ImportChangeDto = {
    /** Строка файла */
    row: number;
    studentId: Guid;
    /** null — задачи с таким номером ещё нет (создастся при createMissingTasks) */
    taskId: Guid | null;
    taskNumber: number;
    kind: ImportChangeKind;
    status: GradebookEntryTaskStatus;
    previousStatus: GradebookEntryTaskStatus | null;
};

export type UnrecognizedRowDto = {
    row: number;
    name: string;
    reason: string;
    /** Похожие ученики группы для ручного сопоставления */
    candidates: StudentDto[];
};

export type ImportPreviewDto = {
    uploadId: Guid;
    fileName: string;
    /** Нет листа _meta — сопоставление только по тексту */
    foreignFile: boolean;
    /** «Файл от занятия 12.09»: применение только с подтверждением */
    lessonMismatch: string | null;
    changes: ImportChangeDto[];
    unrecognizedRows: UnrecognizedRowDto[];
    missingStudents: StudentDto[];
    /** Номера задач из файла, которых нет в занятии */
    missingTaskNumbers: number[];
    /** «D7: «решил почти»» */
    cellErrors: string[];
    warnings: string[];
    /** Блокирующие ошибки (дубль ученика) — применить нельзя */
    errors: string[];
};

/** studentId null — пропустить строку */
export type RowMappingRequest = {
    row: number;
    studentId: Guid | null;
};

export type ApplyImportRequest = {
    confirmLessonMismatch: boolean;
    createMissingTasks: boolean;
    publish: boolean;
    rowMappings: RowMappingRequest[];
};

export type ImportResultDto = {
    created: number;
    updated: number;
    unchanged: number;
    published: boolean;
};

export async function listTeacherGroups(): Promise<TeacherGroupCardDto[]> {
    const response = await api.get<TeacherGroupCardDto[]>("/teacher/groups");
    return response.data;
}

export async function getTeacherGroup(id: Guid): Promise<TeacherGroupDto> {
    const response = await api.get<TeacherGroupDto>(`/teacher/groups/${id}`);
    return response.data;
}

export async function addGroupStudent(
    groupId: Guid,
    request: NewStudentRequest,
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

export async function listLessons(groupId: Guid): Promise<LessonSummaryDto[]> {
    const response = await api.get<LessonSummaryDto[]>(
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

export async function getGroupSummary(groupId: Guid): Promise<GroupMatrixDto> {
    const response = await api.get<GroupMatrixDto>(
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
): Promise<LessonDto> {
    const response = await api.post<LessonDto>(
        `/teacher/lessons/${lessonId}/tasks`,
        { title } satisfies TaskTitleRequest,
    );
    return response.data;
}

export async function renameTask(
    lessonId: Guid,
    taskId: Guid,
    title: string | null,
): Promise<LessonDto> {
    const response = await api.put<LessonDto>(
        `/teacher/lessons/${lessonId}/tasks/${taskId}`,
        { title } satisfies TaskTitleRequest,
    );
    return response.data;
}

export async function deleteTask(
    lessonId: Guid,
    taskId: Guid,
): Promise<LessonDto> {
    const response = await api.delete<LessonDto>(
        `/teacher/lessons/${lessonId}/tasks/${taskId}`,
    );
    return response.data;
}

export async function getGradebook(lessonId: Guid): Promise<GradebookDto> {
    const response = await api.get<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook`,
    );
    return response.data;
}

export async function saveGradebook(
    lessonId: Guid,
    changes: GradebookCellChange[],
    attendance: GradebookAttendanceChange[] = [],
): Promise<GradebookDto> {
    const response = await api.put<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook`,
        { changes, attendance } satisfies SaveGradebookRequest,
    );
    return response.data;
}

export async function publishGradebook(lessonId: Guid): Promise<GradebookDto> {
    const response = await api.post<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook/publish`,
    );
    return response.data;
}

export async function unpublishGradebook(
    lessonId: Guid,
): Promise<GradebookDto> {
    const response = await api.post<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook/unpublish`,
    );
    return response.data;
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
        `/teacher/lessons/${lessonId}/gradebook/imports/${uploadId}/file`,
    );
}

export async function listImports(lessonId: Guid): Promise<ImportFileDto[]> {
    const response = await api.get<ImportFileDto[]>(
        `/teacher/lessons/${lessonId}/gradebook/imports`,
    );
    return response.data;
}

export async function previewImport(
    lessonId: Guid,
    file: File,
): Promise<ImportPreviewDto> {
    const form = new FormData();
    form.append("file", file);
    const response = await api.post<ImportPreviewDto>(
        `/teacher/lessons/${lessonId}/gradebook/import`,
        form,
        { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
}

export async function applyImport(
    lessonId: Guid,
    uploadId: Guid,
    request: ApplyImportRequest,
): Promise<ImportResultDto> {
    const response = await api.post<ImportResultDto>(
        `/teacher/lessons/${lessonId}/gradebook/import/${uploadId}/apply`,
        request,
    );
    return response.data;
}
