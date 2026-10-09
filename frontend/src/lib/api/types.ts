// Общие типы контрактов API. Когда бэкенд опубликует эндпоинты, их стоит
// генерировать из /openapi/v1.json (openapi-typescript), а не писать руками.

export type Guid = string;
/** yyyy-MM-dd */
export type DateOnly = string;
/** HH:mm[:ss] */
export type TimeOnly = string;
/** ISO 8601 с часовым поясом */
export type DateTimeString = string;

export type UserRole = "Parent" | "Teacher" | "Admin";

export type GradeStatus =
    "Solved" | "SolvedWithRemarks" | "UnsolvedWithRemarks" | "Unsolved";

export type PersonName = {
    firstName: string;
    middleName: string | null;
    lastName: string;
};

export type StudentDto = PersonName & {
    id: Guid;
    grade: string | null;
};

export type GroupShortDto = {
    id: Guid;
    title: string;
};

export type GroupRefDto = GroupShortDto & { isArchived: boolean };

export type UserShortDto = PersonName & {
    id: Guid;
    email: string;
};

export type TaskDto = {
    id: Guid;
    order: number;
    title: string | null;
    /** Сколько отметок удалится вместе с задачей */
    entryCount: number;
};

export type AccessRequestStatus =
    "Pending" | "Approved" | "Rejected" | "Cancelled";

export type Relation = "Mother" | "Father" | "Guardian" | "Other";

export type AccessRequestDto = {
    id: Guid;
    group: GroupRefDto;
    childFirstName: string;
    childMiddleName: string | null;
    childLastName: string;
    grade: string | null;
    relation: Relation;
    comment: string | null;
    status: AccessRequestStatus;
    rejectReason: string | null;
    created: DateTimeString;
    decidedAt: DateTimeString | null;
};
