import { api, apiUrl, statusOf } from "$lib/api/client";
import type { Guid, PersonName, UserRole } from "$lib/api/types";

export type MeDto = {
    /** null у сервисного админа: у него нет аккаунта */
    id: Guid | null;
    firstName: string | null;
    middleName: string | null;
    lastName: string | null;
    email: string | null;
    avatarId: Guid | null;
    roles: UserRole[];
    isServiceAdmin: boolean;
    /** Не принята текущая версия политики ПДн — пускаем только на /consent. */
    consentRequired: boolean;
};

export type RegisterRequest = PersonName & {
    email: string;
    phone: string | null;
    password: string;
    acceptPersonalDataPolicy: boolean;
};

export type LoginRequest = {
    email: string;
    password: string;
};

/** null, если сессии нет. */
export async function getMe(): Promise<MeDto | null> {
    try {
        const response = await api.get<MeDto>("/auth/me");
        return response.data;
    } catch (error) {
        if (statusOf(error) === 401) return null;
        throw error;
    }
}

export async function login(request: LoginRequest): Promise<void> {
    await api.post("/auth/login", request);
}

export async function register(request: RegisterRequest): Promise<void> {
    await api.post("/auth/register", request);
}

export async function logout(): Promise<void> {
    await api.post("/auth/logout");
}

export async function adminLogin(password: string): Promise<void> {
    await api.post("/admin/login", { password });
}

export async function adminLogout(): Promise<void> {
    await api.post("/admin/logout");
}

export type ExternalProviderDto = {
    name: string;
    displayName: string;
};

export async function listExternalProviders(): Promise<ExternalProviderDto[]> {
    const response = await api.get<ExternalProviderDto[]>("/auth/providers");
    return response.data;
}

/**
 * Вход через внешний сервис — обычная навигация: backend ведёт OIDC-флоу, ставит cookie
 * и редиректит на returnUrl (или на /consent, или на /login?error=external|blocked).
 */
export function externalLoginUrl(provider: string, returnUrl: string): string {
    return apiUrl(
        `/auth/external/${encodeURIComponent(provider)}?returnUrl=${encodeURIComponent(returnUrl)}`,
    );
}
