import { goto } from "$app/navigation";
import { api, problemOf } from "$lib/api/client";
import { getMe, type MeDto } from "$lib/api/auth";
import type { UserRole } from "$lib/api/types";

export type Section = {
    role: UserRole;
    href: string;
    label: string;
};

export const SECTIONS: Section[] = [
    { role: "Parent", href: "/parent", label: "Родитель" },
    { role: "Teacher", href: "/teacher", label: "Учитель" },
    { role: "Admin", href: "/admin", label: "Админ" },
];

class Session {
    me = $state<MeDto | null>(null);
    loaded = $state(false);

    sections = $derived(
        SECTIONS.filter((section) => this.me?.roles.includes(section.role)),
    );

    #pending: Promise<MeDto | null> | null = null;

    /** Один запрос /auth/me на все гварды; force — после входа, выхода и согласия. */
    async load(force = false): Promise<MeDto | null> {
        if (this.loaded && !force) return this.me;
        this.#pending ??= getMe().finally(() => (this.#pending = null));
        this.me = await this.#pending;
        this.loaded = true;
        return this.me;
    }

    hasRole(role: UserRole): boolean {
        return this.me?.roles.includes(role) ?? false;
    }

    clear() {
        this.me = null;
        this.loaded = true;
    }

    /** Раздел по умолчанию после входа. */
    homeHref(): string {
        return this.sections[0]?.href ?? "/account";
    }
}

export const session = new Session();

// сервер — источник правды: истёкшая сессия и непринятое согласие ловятся на любом запросе
api.interceptors.response.use(undefined, (error) => {
    const problem = problemOf(error);
    const url: string = error?.config?.url ?? "";
    const path = window.location.pathname;

    if (problem?.status === 403 && problem.code === "consent_required") {
        if (path !== "/consent") goto("/consent");
    } else if (
        problem?.status === 401 &&
        !url.startsWith("/auth/") &&
        !url.startsWith("/admin/login") &&
        session.me
    ) {
        session.clear();
        const login = path.startsWith("/admin") ? "/admin/login" : "/login";
        goto(`${login}?returnUrl=${encodeURIComponent(path)}`);
    }

    return Promise.reject(error);
});
