import { api } from "$lib/api/client";
import type {
    AccessRequestDto,
    AccessRequestStatus,
    DateOnly,
    DateTimeString,
    GroupRefDto,
    Guid,
    NewStudentRequest,
    PersonName,
    StudentDto,
    UserRefDto,
    UserRole,
} from "$lib/api/types";

// --- запросы доступа

export type RequestParentDto = {
    id: Guid;
    lastName: string;
    firstName: string;
    middleName: string;
    email: string;
    phone: string | null;
    children: StudentDto[];
};

export type AdminAccessRequestDto = AccessRequestDto & {
    parent: RequestParentDto;
    /** Ученик, к которому привязан одобренный запрос */
    student: StudentDto | null;
    /** Похожие ученики из указанной группы */
    suggestions: StudentDto[];
};

export type PendingCountDto = {
    count: number;
};

export type ApproveAccessRequestRequest =
    | { studentId: Guid; newStudent: null }
    | { studentId: null; newStudent: NewStudentRequest };

export type RejectAccessRequestRequest = {
    reason: string;
};

// --- группы

export type GroupSummaryDto = {
    id: Guid;
    title: string;
    description: string | null;
    isArchived: boolean;
    teachers: UserRefDto[];
    studentCount: number;
};

export type GroupDetailsDto = {
    id: Guid;
    title: string;
    description: string | null;
    isArchived: boolean;
    teachers: UserRefDto[];
    students: StudentDto[];
};

export type CreateGroupRequest = {
    title: string;
    description: string | null;
};

export type UpdateGroupRequest = CreateGroupRequest & {
    isArchived: boolean;
};

export type AddGroupStudentsRequest = {
    studentIds: Guid[];
    newStudents: NewStudentRequest[];
};

// --- пользователи

export type UserSummaryDto = {
    id: Guid;
    lastName: string;
    firstName: string;
    middleName: string;
    email: string;
    phone: string | null;
    roles: UserRole[];
    isBlocked: boolean;
};

export type UserDetailsDto = UserSummaryDto & {
    consentAcceptedAt: DateTimeString | null;
    loginProviders: string[];
    children: StudentDto[];
    groups: GroupRefDto[];
};

export type SetRolesRequest = {
    roles: UserRole[];
};

export type TemporaryPasswordDto = {
    password: string;
};

export type InviteTeacherRequest = PersonName & {
    email: string;
};

// --- ученики

export type StudentListItemDto = StudentDto & {
    groups: GroupRefDto[];
};

export type StudentDetailsDto = StudentListItemDto & {
    parents: UserRefDto[];
    gradebookEntryCount: number;
};

export type CreateStudentRequest = NewStudentRequest & {
    groupId: Guid | null;
};

export type UpdateStudentRequest = NewStudentRequest;

export type ParseNamesRequest = {
    text: string;
};

/** Строка вставленного списка ФИО */
export type ParsedNameDto = {
    /** С 1 */
    line: number;
    source: string;
    /** null — строку не разобрать, см. error */
    student: NewStudentRequest | null;
    error: string | null;
    /** Уже существующие ученики с теми же фамилией и именем */
    matches: StudentDto[];
};

export type MergeStudentsResultDto = {
    targetId: Guid;
    movedEntries: number;
    /** Отметки дубля, у цели уже была отметка по той же задаче */
    droppedEntries: number;
};

// --- удаление ПДн: на бэкенде эндпоинтов пока нет, контракт предварительный

export type ErasurePreviewDto = {
    entries: number;
    groups: number;
    parents: number;
    files: number;
};

export type ErasureRecordDto = {
    id: Guid;
    requestReceivedAt: DateOnly;
    erasedAt: DateTimeString;
    filesCleaned: boolean;
};

// --- API: запросы доступа

/** Запросы доступа, ждущие решения, — бейдж в навигации */
export async function getPendingRequestCount(): Promise<number> {
    const response = await api.get<PendingCountDto>(
        "/admin/access-requests/pending-count",
    );
    return response.data.count;
}

export async function listAdminRequests(
    status: AccessRequestStatus | null,
): Promise<AdminAccessRequestDto[]> {
    const response = await api.get<AdminAccessRequestDto[]>(
        "/admin/access-requests",
        { params: { status: status ?? undefined } },
    );
    return response.data;
}

export async function approveRequest(
    id: Guid,
    request: ApproveAccessRequestRequest,
): Promise<void> {
    await api.post(`/admin/access-requests/${id}/approve`, request);
}

export async function rejectRequest(id: Guid, reason: string): Promise<void> {
    await api.post(`/admin/access-requests/${id}/reject`, {
        reason,
    } satisfies RejectAccessRequestRequest);
}

// --- группы

export async function listAdminGroups(
    includeArchived = false,
): Promise<GroupSummaryDto[]> {
    const response = await api.get<GroupSummaryDto[]>("/admin/groups", {
        params: { includeArchived: includeArchived || undefined },
    });
    return response.data;
}

export async function getAdminGroup(id: Guid): Promise<GroupDetailsDto> {
    const response = await api.get<GroupDetailsDto>(`/admin/groups/${id}`);
    return response.data;
}

export async function createGroup(
    request: CreateGroupRequest,
): Promise<GroupDetailsDto> {
    const response = await api.post<GroupDetailsDto>("/admin/groups", request);
    return response.data;
}

export async function updateGroup(
    id: Guid,
    request: UpdateGroupRequest,
): Promise<GroupDetailsDto> {
    const response = await api.put<GroupDetailsDto>(
        `/admin/groups/${id}`,
        request,
    );
    return response.data;
}

/** Только группу без занятий */
export async function deleteGroup(id: Guid): Promise<void> {
    await api.delete(`/admin/groups/${id}`);
}

export async function addGroupTeacher(
    groupId: Guid,
    teacherId: Guid,
): Promise<GroupDetailsDto> {
    const response = await api.post<GroupDetailsDto>(
        `/admin/groups/${groupId}/teachers/${teacherId}`,
    );
    return response.data;
}

export async function removeGroupTeacher(
    groupId: Guid,
    teacherId: Guid,
): Promise<GroupDetailsDto> {
    const response = await api.delete<GroupDetailsDto>(
        `/admin/groups/${groupId}/teachers/${teacherId}`,
    );
    return response.data;
}

/** Существующих учеников — по id, новых (в т.ч. массовая вставка) — списком ФИО. */
export async function addAdminGroupStudents(
    groupId: Guid,
    studentIds: Guid[],
    newStudents: NewStudentRequest[],
): Promise<GroupDetailsDto> {
    const response = await api.post<GroupDetailsDto>(
        `/admin/groups/${groupId}/students`,
        { studentIds, newStudents } satisfies AddGroupStudentsRequest,
    );
    return response.data;
}

/** Отметки ученика остаются */
export async function removeAdminGroupStudent(
    groupId: Guid,
    studentId: Guid,
): Promise<GroupDetailsDto> {
    const response = await api.delete<GroupDetailsDto>(
        `/admin/groups/${groupId}/students/${studentId}`,
    );
    return response.data;
}

// --- пользователи

export async function listUsers(
    q: string,
    role: UserRole | null = null,
): Promise<UserSummaryDto[]> {
    const response = await api.get<UserSummaryDto[]>("/admin/users", {
        params: { q: q || undefined, role: role ?? undefined },
    });
    return response.data;
}

export async function getUser(id: Guid): Promise<UserDetailsDto> {
    const response = await api.get<UserDetailsDto>(`/admin/users/${id}`);
    return response.data;
}

export async function setUserRoles(
    id: Guid,
    roles: UserRole[],
): Promise<UserDetailsDto> {
    const response = await api.put<UserDetailsDto>(`/admin/users/${id}/roles`, {
        roles,
    } satisfies SetRolesRequest);
    return response.data;
}

/** Блокировка завершает сессии пользователя */
export async function setUserBlocked(
    id: Guid,
    blocked: boolean,
): Promise<UserDetailsDto> {
    const response = await api.post<UserDetailsDto>(
        `/admin/users/${id}/${blocked ? "block" : "unblock"}`,
    );
    return response.data;
}

/** Временный пароль показывается один раз */
export async function resetUserPassword(id: Guid): Promise<string> {
    const response = await api.post<TemporaryPasswordDto>(
        `/admin/users/${id}/reset-password`,
    );
    return response.data.password;
}

/** Создать учителя по email или выдать роль существующему пользователю */
export async function inviteTeacher(
    request: InviteTeacherRequest,
): Promise<UserDetailsDto> {
    const response = await api.post<UserDetailsDto>(
        "/admin/teachers/invite",
        request,
    );
    return response.data;
}

export async function linkChild(
    parentId: Guid,
    studentId: Guid,
): Promise<UserDetailsDto> {
    const response = await api.post<UserDetailsDto>(
        `/admin/parents/${parentId}/children/${studentId}`,
    );
    return response.data;
}

export async function unlinkChild(
    parentId: Guid,
    studentId: Guid,
): Promise<UserDetailsDto> {
    const response = await api.delete<UserDetailsDto>(
        `/admin/parents/${parentId}/children/${studentId}`,
    );
    return response.data;
}

// --- ученики

export async function listStudents(
    q: string,
    groupId: Guid | null = null,
): Promise<StudentListItemDto[]> {
    const response = await api.get<StudentListItemDto[]>("/admin/students", {
        params: { q: q || undefined, groupId: groupId ?? undefined },
    });
    return response.data;
}

export async function getStudent(id: Guid): Promise<StudentDetailsDto> {
    const response = await api.get<StudentDetailsDto>(`/admin/students/${id}`);
    return response.data;
}

export async function createStudent(
    request: CreateStudentRequest,
): Promise<StudentDetailsDto> {
    const response = await api.post<StudentDetailsDto>(
        "/admin/students",
        request,
    );
    return response.data;
}

export async function updateStudent(
    id: Guid,
    request: UpdateStudentRequest,
): Promise<StudentDetailsDto> {
    const response = await api.put<StudentDetailsDto>(
        `/admin/students/${id}`,
        request,
    );
    return response.data;
}

/** Только ученика без отметок; с отметками — удаление ПДн */
export async function deleteStudent(id: Guid): Promise<void> {
    await api.delete(`/admin/students/${id}`);
}

export async function mergeStudents(
    sourceId: Guid,
    targetId: Guid,
): Promise<MergeStudentsResultDto> {
    const response = await api.post<MergeStudentsResultDto>(
        `/admin/students/${sourceId}/merge-into/${targetId}`,
    );
    return response.data;
}

export async function parseNames(text: string): Promise<ParsedNameDto[]> {
    const response = await api.post<ParsedNameDto[]>(
        "/admin/students/parse-names",
        { text } satisfies ParseNamesRequest,
    );
    return response.data;
}

/** Первый лист .xlsx: строка — ученик (ФИО одной ячейкой или по колонкам); line — номер строки листа */
export async function parseNamesXlsx(file: File): Promise<ParsedNameDto[]> {
    const form = new FormData();
    form.append("file", file);
    const response = await api.post<ParsedNameDto[]>(
        "/admin/students/parse-xlsx",
        form,
        { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
}

export async function getErasurePreview(id: Guid): Promise<ErasurePreviewDto> {
    const response = await api.get<ErasurePreviewDto>(
        `/admin/students/${id}/erase`,
    );
    return response.data;
}

export async function eraseStudent(
    id: Guid,
    requestReceivedAt: DateOnly,
    confirmLastName: string,
): Promise<void> {
    await api.post(`/admin/students/${id}/erase`, {
        requestReceivedAt,
        confirmLastName,
    });
}

export async function listPendingErasures(): Promise<ErasureRecordDto[]> {
    const response = await api.get<ErasureRecordDto[]>("/admin/erasures", {
        params: { filesCleaned: false },
    });
    return response.data;
}

export async function retryErasure(id: Guid): Promise<void> {
    await api.post(`/admin/erasures/${id}/retry`);
}
