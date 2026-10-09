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

export type TeacherGroupDto = {
    id: Guid;
    title: string;
    description: string | null;
    studentCount: number;
    lastLessonDate: DateOnly | null;
    nextLessonDate: DateOnly | null;
    /** Прошедшие занятия (включая сегодня) с неполностью заполненным журналом */
    unfilledLessonCount: number;
};

export type LessonListItemDto = {
    id: Guid;
    date: DateOnly;
    time: TimeOnly | null;
    topic: string | null;
    taskCount: number;
    /** Доля заполненных ячеек для текущего состава, 0–100 */
    filledPercent: number;
    isPublished: boolean;
};

export type TeacherGroupDetailsDto = {
    id: Guid;
    title: string;
    description: string | null;
    isArchived: boolean;
    students: StudentDto[];
    lessons: LessonListItemDto[];
};

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

/** inGroup false — выбывший ученик, у которого остались отметки */
export type GradebookStudentDto = StudentDto & { inGroup: boolean };

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
    date: DateOnly;
    topic: string | null;
    publishedAt: DateTimeString | null;
    tasks: TaskDto[];
    students: GradebookStudentDto[];
    entries: GradebookEntryDto[];
    /** Загруженные XLSX — появятся с импортом */
    imports?: ImportFileDto[];
};

export type GradebookChangeDto = {
    studentId: Guid;
    taskId: Guid;
    /** null — очистить ячейку */
    status: GradeStatus | null;
    /** Ячейка задаётся целиком: null убирает комментарий */
    comment: string | null;
};

export type GroupMatrixDto = {
    lessons: {
        id: Guid;
        date: DateOnly;
        topic: string | null;
        taskCount: number;
        isPublished: boolean;
    }[];
    students: GradebookStudentDto[];
    /** Ячейка есть, только если у ученика есть отметки на занятии */
    cells: {
        studentId: Guid;
        lessonId: Guid;
        marked: number;
        /** Решено, в том числе с замечаниями */
        solved: number;
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
        { title },
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
        { title },
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
    changes: GradebookChangeDto[],
): Promise<GradebookDto> {
    const response = await api.put<GradebookDto>(
        `/teacher/lessons/${lessonId}/gradebook`,
        { changes },
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
