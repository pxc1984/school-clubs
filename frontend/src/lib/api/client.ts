import axios, { isAxiosError } from "axios";

import { API_URL } from "$lib/config";

export const api = axios.create({
    baseURL: `${API_URL}/api/v1`,
    // cookie сессии HttpOnly и уходит сама; заголовок — защита от CSRF
    withCredentials: true,
    headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "fetch",
    },
});

/** Абсолютный путь к эндпоинту — для ссылок `<a href download>` и редиректов OAuth. */
export function apiUrl(path: string): string {
    return `${API_URL}/api/v1${path}`;
}

export type ProblemDetails = {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    /** Машиночитаемая причина: `consent_required`, `blocked`, … */
    code?: string;
    errors?: Record<string, string[]>;
};

export function problemOf(error: unknown): ProblemDetails | null {
    if (!isAxiosError(error) || !error.response) return null;
    const data = error.response.data as ProblemDetails | undefined;
    return { ...(data ?? {}), status: error.response.status };
}

export function statusOf(error: unknown): number | null {
    return problemOf(error)?.status ?? null;
}

export function errorMessage(
    error: unknown,
    fallback = "Что-то пошло не так. Попробуйте ещё раз.",
): string {
    if (isAxiosError(error) && !error.response)
        return "Сервер недоступен. Проверьте подключение.";

    const problem = problemOf(error);
    if (!problem) return fallback;

    const firstFieldError = Object.values(problem.errors ?? {})[0]?.[0];
    return problem.detail || firstFieldError || problem.title || fallback;
}

/** Ошибки валидации по полям; ключи приведены к camelCase, как в формах. */
export function fieldErrors(error: unknown): Record<string, string> {
    const result: Record<string, string> = {};
    for (const [key, messages] of Object.entries(
        problemOf(error)?.errors ?? {},
    )) {
        const name = key.charAt(0).toLowerCase() + key.slice(1);
        if (messages[0]) result[name] = messages[0];
    }
    return result;
}

/** Только локальный путь — как и на backend, защита от open redirect. */
export function safeReturnUrl(value: string | null | undefined): string | null {
    if (
        !value ||
        !value.startsWith("/") ||
        value.startsWith("//") ||
        value.startsWith("/\\")
    )
        return null;
    return value;
}
