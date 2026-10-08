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

export type AdminGroupDto = GroupShortDto & {
    description: string | null;
    isArchived: boolean;
    studentCount: number;
    teachers: UserShortDto[];
};

export type AdminGroupDetailsDto = AdminGroupDto & {
    students: StudentDto[];
};

export type SaveGroupRequest = {
    title: string;
    description: string | null;
    isArchived: boolean;
};

export type AdminUserDto = UserShortDto & {
    phone: string | null;
    roles: UserRole[];
    isBlocked: boolean;
    children: StudentDto[];
    groups: GroupShortDto[];
};

export type InviteTeacherRequest = PersonName & {
    email: string;
};

export type AdminStudentDto = StudentDto & {
    groups: GroupShortDto[];
    parents: UserShortDto[];
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

export async function listAdminGroups(): Promise<AdminGroupDto[]> {
    const response = await api.get<AdminGroupDto[]>("/admin/groups");
    return response.data;
}

export async function getAdminGroup(id: Guid): Promise<AdminGroupDetailsDto> {
    const response = await api.get<AdminGroupDetailsDto>(`/admin/groups/${id}`);
    return response.data;
}

export async function createGroup(
    request: SaveGroupRequest,
): Promise<AdminGroupDto> {
    const response = await api.post<AdminGroupDto>("/admin/groups", request);
    return response.data;
}

export async function updateGroup(
    id: Guid,
    request: SaveGroupRequest,
): Promise<AdminGroupDto> {
    const response = await api.put<AdminGroupDto>(
        `/admin/groups/${id}`,
        request,
    );
    return response.data;
}

export async function addGroupTeacher(
    groupId: Guid,
    teacherId: Guid,
): Promise<void> {
    await api.post(`/admin/groups/${groupId}/teachers/${teacherId}`);
}

export async function removeGroupTeacher(
    groupId: Guid,
    teacherId: Guid,
): Promise<void> {
    await api.delete(`/admin/groups/${groupId}/teachers/${teacherId}`);
}

/** Существующих учеников — по id, новых (в т.ч. массовая вставка) — списком ФИО. */
export async function addAdminGroupStudents(
    groupId: Guid,
    studentIds: Guid[],
    newStudents: NewStudentRequest[],
): Promise<void> {
    await api.post(`/admin/groups/${groupId}/students`, {
        studentIds,
        newStudents,
    });
}

export async function removeAdminGroupStudent(
    groupId: Guid,
    studentId: Guid,
): Promise<void> {
    await api.delete(`/admin/groups/${groupId}/students`, {
        data: { studentIds: [studentId] },
    });
}

// --- пользователи

export async function listUsers(
    search: string,
    role: UserRole | null = null,
): Promise<AdminUserDto[]> {
    const response = await api.get<AdminUserDto[]>("/admin/users", {
        params: { search: search || undefined, role: role ?? undefined },
    });
    return response.data;
}

export async function setUserRoles(id: Guid, roles: UserRole[]): Promise<void> {
    await api.put(`/admin/users/${id}/roles`, { roles });
}

export async function setUserBlocked(
    id: Guid,
    blocked: boolean,
): Promise<void> {
    await api.post(`/admin/users/${id}/block`, { blocked });
}

export async function resetUserPassword(
    id: Guid,
): Promise<{ temporaryPassword: string }> {
    const response = await api.post<{ temporaryPassword: string }>(
        `/admin/users/${id}/reset-password`,
    );
    return response.data;
}

export async function deleteUser(id: Guid): Promise<void> {
    await api.delete(`/admin/users/${id}`);
}

export async function inviteTeacher(
    request: InviteTeacherRequest,
): Promise<void> {
    await api.post("/admin/teachers/invite", request);
}

export async function unlinkChild(
    parentId: Guid,
    studentId: Guid,
): Promise<void> {
    await api.delete(`/admin/parents/${parentId}/children/${studentId}`);
}

// --- ученики

export async function listStudents(search: string): Promise<AdminStudentDto[]> {
    const response = await api.get<AdminStudentDto[]>("/admin/students", {
        params: { search: search || undefined },
    });
    return response.data;
}

export async function createStudent(
    request: NewStudentRequest,
): Promise<StudentDto> {
    const response = await api.post<StudentDto>("/admin/students", request);
    return response.data;
}

export async function updateStudent(
    id: Guid,
    request: NewStudentRequest,
): Promise<StudentDto> {
    const response = await api.put<StudentDto>(
        `/admin/students/${id}`,
        request,
    );
    return response.data;
}

export async function mergeStudents(
    sourceId: Guid,
    targetId: Guid,
): Promise<void> {
    await api.post(`/admin/students/${sourceId}/merge-into/${targetId}`);
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
