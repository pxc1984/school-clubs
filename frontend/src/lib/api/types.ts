// Общие типы контрактов API. Имена и поля один в один со схемами /openapi/v1.json бэкенда;
// типы конкретных разделов — в соседних модулях (auth, account, admin, parent, teacher).

export type Guid = string;
/** yyyy-MM-dd */
export type DateOnly = string;
/** HH:mm[:ss] */
export type TimeOnly = string;
/** ISO 8601 с часовым поясом */
export type DateTimeString = string;

/** Не схема API: ФИО в формах и запросах, отчество необязательно. */
export type PersonName = {
    lastName: string;
    firstName: string;
    middleName: string | null;
};

export type UserRole = "Parent" | "Teacher" | "Admin";

export type GradebookEntryTaskStatus =
    "UnsolvedWithRemarks" | "Unsolved" | "SolvedWithRemarks" | "Solved";

export type ParentRelation = "Mother" | "Father" | "Guardian" | "Other";

export type AccessRequestStatus =
    "Pending" | "Approved" | "Rejected" | "Cancelled";

export type StudentDto = {
    id: Guid;
    lastName: string;
    firstName: string;
    /** Пустая строка, если отчества нет */
    middleName: string;
    grade: string | null;
};

export type NewStudentRequest = PersonName & {
    grade: string | null;
};

export type GroupRefDto = {
    id: Guid;
    title: string;
    isArchived: boolean;
};

export type UserRefDto = {
    id: Guid;
    lastName: string;
    firstName: string;
    middleName: string;
};

export type TaskDto = {
    id: Guid;
    order: number;
    title: string | null;
    /** Сколько отметок удалится вместе с задачей */
    entryCount: number;
};

export type AccessRequestDto = {
    id: Guid;
    group: GroupRefDto;
    childLastName: string;
    childFirstName: string;
    childMiddleName: string | null;
    grade: string | null;
    relation: ParentRelation;
    comment: string | null;
    status: AccessRequestStatus;
    rejectReason: string | null;
    created: DateTimeString;
    decidedAt: DateTimeString | null;
};
