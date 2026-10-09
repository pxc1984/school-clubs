import { api } from "$lib/api/client";
import type {
    AccessRequestDto,
    AccessRequestStatus,
    DateOnly,
    DateTimeString,
    GroupShortDto,
    Guid,
    PersonName,
    StudentDto,
    UserRole,
    UserShortDto,
} from "$lib/api/types";

export type AdminCountersDto = {
    pendingRequests: number;
    /** Удаления ПДн, у которых не дочищены файлы в S3 */
    pendingErasures: number;
};

export type AdminAccessRequestDto = AccessRequestDto & {
    parent: UserShortDto & {
        phone: string | null;
        children: StudentDto[];
    };
    /** Похожие ученики из указанной группы */
    suggestions: StudentDto[];
};

export type NewStudentRequest = PersonName & {
    grade: string | null;
};

export type ApproveRequest =
    | { studentId: Guid; newStudent: null }
    | { studentId: null; newStudent: NewStudentRequest };

export type UserRefDto = PersonName & { id: Guid };

export type GroupRefDto = GroupShortDto & { isArchived: boolean };

export type AdminGroupDto = GroupShortDto & {
    description: string | null;
    isArchived: boolean;
    teachers: UserRefDto[];
    studentCount: number;
};

export type AdminGroupDetailsDto = GroupShortDto & {
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

export type AdminUserDto = PersonName & {
    id: Guid;
    email: string;
    phone: string | null;
    roles: UserRole[];
    isBlocked: boolean;
};

export type AdminUserDetailsDto = AdminUserDto & {
    consentAcceptedAt: DateTimeString | null;
    loginProviders: string[];
    children: StudentDto[];
    groups: GroupRefDto[];
};

export type InviteTeacherRequest = PersonName & {
    email: string;
};

export type AdminStudentDto = StudentDto & {
    groups: GroupRefDto[];
};

export type AdminStudentDetailsDto = AdminStudentDto & {
    parents: UserRefDto[];
    gradebookEntryCount: number;
};

export type CreateStudentRequest = NewStudentRequest & {
    groupId: Guid | null;
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

export async function getCounters(): Promise<AdminCountersDto> {
    const response = await api.get<AdminCountersDto>("/admin/counters");
    return response.data;
}

// --- запросы доступа

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
    request: ApproveRequest,
): Promise<void> {
    await api.post(`/admin/access-requests/${id}/approve`, request);
}

export async function rejectRequest(id: Guid, reason: string): Promise<void> {
    await api.post(`/admin/access-requests/${id}/reject`, { reason });
}

// --- группы

export async function listAdminGroups(
    includeArchived = false,
): Promise<AdminGroupDto[]> {
    const response = await api.get<AdminGroupDto[]>("/admin/groups", {
        params: { includeArchived: includeArchived || undefined },
    });
    return response.data;
}

export async function getAdminGroup(id: Guid): Promise<AdminGroupDetailsDto> {
    const response = await api.get<AdminGroupDetailsDto>(`/admin/groups/${id}`);
    return response.data;
}

export async function createGroup(
    request: CreateGroupRequest,
): Promise<AdminGroupDetailsDto> {
    const response = await api.post<AdminGroupDetailsDto>(
        "/admin/groups",
        request,
    );
    return response.data;
}

export async function updateGroup(
    id: Guid,
    request: UpdateGroupRequest,
): Promise<AdminGroupDetailsDto> {
    const response = await api.put<AdminGroupDetailsDto>(
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
): Promise<AdminGroupDetailsDto> {
    const response = await api.post<AdminGroupDetailsDto>(
        `/admin/groups/${groupId}/teachers/${teacherId}`,
    );
    return response.data;
}

export async function removeGroupTeacher(
    groupId: Guid,
    teacherId: Guid,
): Promise<AdminGroupDetailsDto> {
    const response = await api.delete<AdminGroupDetailsDto>(
        `/admin/groups/${groupId}/teachers/${teacherId}`,
    );
    return response.data;
}

/** Существующих учеников — по id, новых (в т.ч. массовая вставка) — списком ФИО. */
export async function addAdminGroupStudents(
    groupId: Guid,
    studentIds: Guid[],
    newStudents: NewStudentRequest[],
): Promise<AdminGroupDetailsDto> {
    const response = await api.post<AdminGroupDetailsDto>(
        `/admin/groups/${groupId}/students`,
        { studentIds, newStudents },
    );
    return response.data;
}

/** Отметки ученика остаются */
export async function removeAdminGroupStudent(
    groupId: Guid,
    studentId: Guid,
): Promise<AdminGroupDetailsDto> {
    const response = await api.delete<AdminGroupDetailsDto>(
        `/admin/groups/${groupId}/students/${studentId}`,
    );
    return response.data;
}

// --- пользователи

export async function listUsers(
    q: string,
    role: UserRole | null = null,
): Promise<AdminUserDto[]> {
    const response = await api.get<AdminUserDto[]>("/admin/users", {
        params: { q: q || undefined, role: role ?? undefined },
    });
    return response.data;
}

export async function getUser(id: Guid): Promise<AdminUserDetailsDto> {
    const response = await api.get<AdminUserDetailsDto>(`/admin/users/${id}`);
    return response.data;
}

export async function setUserRoles(
    id: Guid,
    roles: UserRole[],
): Promise<AdminUserDetailsDto> {
    const response = await api.put<AdminUserDetailsDto>(
        `/admin/users/${id}/roles`,
        { roles },
    );
    return response.data;
}

/** Блокировка завершает сессии пользователя */
export async function setUserBlocked(
    id: Guid,
    blocked: boolean,
): Promise<AdminUserDetailsDto> {
    const response = await api.post<AdminUserDetailsDto>(
        `/admin/users/${id}/${blocked ? "block" : "unblock"}`,
    );
    return response.data;
}

/** Временный пароль показывается один раз */
export async function resetUserPassword(id: Guid): Promise<string> {
    const response = await api.post<{ password: string }>(
        `/admin/users/${id}/reset-password`,
    );
    return response.data.password;
}

/** Создать учителя по email или выдать роль существующему пользователю */
export async function inviteTeacher(
    request: InviteTeacherRequest,
): Promise<AdminUserDetailsDto> {
    const response = await api.post<AdminUserDetailsDto>(
        "/admin/teachers/invite",
        request,
    );
    return response.data;
}

export async function linkChild(
    parentId: Guid,
    studentId: Guid,
): Promise<AdminUserDetailsDto> {
    const response = await api.post<AdminUserDetailsDto>(
        `/admin/parents/${parentId}/children/${studentId}`,
    );
    return response.data;
}

export async function unlinkChild(
    parentId: Guid,
    studentId: Guid,
): Promise<AdminUserDetailsDto> {
    const response = await api.delete<AdminUserDetailsDto>(
        `/admin/parents/${parentId}/children/${studentId}`,
    );
    return response.data;
}

// --- ученики

export async function listStudents(
    q: string,
    groupId: Guid | null = null,
): Promise<AdminStudentDto[]> {
    const response = await api.get<AdminStudentDto[]>("/admin/students", {
        params: { q: q || undefined, groupId: groupId ?? undefined },
    });
    return response.data;
}

export async function getStudent(id: Guid): Promise<AdminStudentDetailsDto> {
    const response = await api.get<AdminStudentDetailsDto>(
        `/admin/students/${id}`,
    );
    return response.data;
}

export async function createStudent(
    request: CreateStudentRequest,
): Promise<AdminStudentDetailsDto> {
    const response = await api.post<AdminStudentDetailsDto>(
        "/admin/students",
        request,
    );
    return response.data;
}

export async function updateStudent(
    id: Guid,
    request: NewStudentRequest,
): Promise<AdminStudentDetailsDto> {
    const response = await api.put<AdminStudentDetailsDto>(
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
        { text },
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
