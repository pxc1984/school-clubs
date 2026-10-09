import { api, apiUrl } from "$lib/api/client";
import type { Guid, PersonName, UserRole } from "$lib/api/types";

export type AccountDto = PersonName & {
    id: Guid;
    email: string;
    /** без "@", в нижнем регистре; подтягивается из Telegram/Яндекса, если не задан */
    username: string | null;
    phone: string | null;
    avatarId: Guid | null;
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

/** null или пустая строка убирает ник; 409 — ник занят */
export async function changeUsername(
    username: string | null,
): Promise<AccountDto> {
    const response = await api.put<AccountDto>("/account/username", {
        username,
    });
    return response.data;
}

/** PNG, JPEG, GIF или WebP до 2 МБ — тот же лимит, что на backend */
export const AVATAR_MAX_BYTES = 2 * 1024 * 1024;
export const AVATAR_ACCEPT = "image/png,image/jpeg,image/gif,image/webp";

export function avatarUrl(
    avatarId: Guid | null | undefined,
): string | undefined {
    return avatarId ? apiUrl(`/avatars/${avatarId}`) : undefined;
}

export async function uploadAvatar(file: File): Promise<{ id: Guid }> {
    const form = new FormData();
    form.append("file", file);
    const response = await api.put<{ id: Guid }>("/account/avatar", form, {
        headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data;
}

export async function deleteAvatar(): Promise<void> {
    await api.delete("/account/avatar");
}

export async function changePassword(
    request: ChangePasswordRequest,
): Promise<void> {
    await api.post("/account/password", request);
}

/** Полная навигация: backend уводит к провайдеру и возвращает на /account?linked=… или ?linkError=… */
export function linkProviderUrl(provider: string): string {
    return apiUrl(`/account/logins/${encodeURIComponent(provider)}/link`);
}

export async function unlinkProvider(provider: string): Promise<void> {
    await api.delete(`/account/logins/${encodeURIComponent(provider)}`);
}

export async function acceptConsent(): Promise<void> {
    await api.post("/account/consent", { accept: true });
}
