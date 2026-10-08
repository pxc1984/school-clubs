import { api } from "$lib/api/client";
import type { Guid, PersonName, UserRole } from "$lib/api/types";

export type AccountDto = PersonName & {
    id: Guid;
    email: string;
    phone: string | null;
    roles: UserRole[];
    /** "password" и имена внешних провайдеров ("yandex") */
    loginProviders: string[];
};

export type UpdateAccountRequest = PersonName & {
    phone: string | null;
};

export type ChangePasswordRequest = {
    /** null, если пароля ещё нет (вход только через Яндекс) */
    currentPassword: string | null;
    newPassword: string;
};

export async function getAccount(): Promise<AccountDto> {
    const response = await api.get<AccountDto>("/account");
    return response.data;
}

export async function updateAccount(
    request: UpdateAccountRequest,
): Promise<AccountDto> {
    const response = await api.put<AccountDto>("/account", request);
    return response.data;
}

export async function changePassword(
    request: ChangePasswordRequest,
): Promise<void> {
    await api.post("/account/password", request);
}

export async function unlinkProvider(provider: string): Promise<void> {
    await api.delete(`/account/logins/${encodeURIComponent(provider)}`);
}

export async function acceptConsent(): Promise<void> {
    await api.post("/account/consent", { accept: true });
}
