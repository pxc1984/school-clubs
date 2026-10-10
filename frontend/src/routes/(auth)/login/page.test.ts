import { render, screen, waitFor } from "@testing-library/svelte";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Page from "./+page.svelte";

const navigation = vi.hoisted(() => ({ goto: vi.fn() }));
vi.mock("$app/navigation", () => navigation);

const state = vi.hoisted(() => ({
    page: { url: new URL("http://localhost/login") },
}));
vi.mock("$app/state", () => state);

const auth = vi.hoisted(() => ({
    login: vi.fn(),
    listExternalProviders: vi.fn(),
    externalLoginUrl: vi.fn(
        (provider: string, returnUrl: string) =>
            `/api/v1/auth/external/${provider}?returnUrl=${encodeURIComponent(returnUrl)}`,
    ),
}));
vi.mock("$lib/api/auth", () => auth);

const session = vi.hoisted(() => ({
    load: vi.fn(),
    homeHref: vi.fn(() => "/parent"),
}));
vi.mock("$lib/session.svelte", () => ({ session }));

const INVITE = "/invite/tok-123";

async function logIn() {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText("Email"), "maria@example.com");
    await user.type(screen.getByLabelText("Пароль"), "correct-horse-battery");
    await user.click(screen.getByRole("button", { name: "Войти" }));
}

describe("вход по ссылке-приглашению", () => {
    beforeEach(() => {
        state.page.url = new URL(
            `http://localhost/login?returnUrl=${encodeURIComponent(INVITE)}`,
        );
        auth.login.mockResolvedValue(undefined);
        auth.listExternalProviders.mockResolvedValue([]);
    });

    it("существующий аккаунт: после входа возвращает на ссылку", async () => {
        session.load.mockResolvedValue({
            roles: ["Parent"],
            consentRequired: false,
        });
        render(Page);

        await logIn();

        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith(INVITE),
        );
        expect(auth.login).toHaveBeenCalledWith({
            email: "maria@example.com",
            password: "correct-horse-battery",
        });
    });

    it("без согласия на ПДн: ведёт на /consent, не теряя ссылку", async () => {
        session.load.mockResolvedValue({
            roles: ["Parent"],
            consentRequired: true,
        });
        render(Page);

        await logIn();

        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith(
                `/consent?returnUrl=${encodeURIComponent(INVITE)}`,
            ),
        );
    });

    it("«Зарегистрироваться» передаёт ссылку на страницу регистрации", () => {
        render(Page);

        expect(
            screen.getByRole("link", { name: "Зарегистрироваться" }),
        ).toHaveAttribute(
            "href",
            `/register?returnUrl=${encodeURIComponent(INVITE)}`,
        );
    });

    it("вход через внешний сервис тоже возвращает на ссылку", async () => {
        auth.listExternalProviders.mockResolvedValue([
            { name: "yandex", displayName: "Яндекс ID" },
        ]);
        render(Page);

        await waitFor(() =>
            expect(auth.externalLoginUrl).toHaveBeenCalledWith(
                "yandex",
                INVITE,
            ),
        );
    });

    it("чужой returnUrl игнорируется", async () => {
        state.page.url = new URL(
            "http://localhost/login?returnUrl=%2F%2Fevil.example",
        );
        session.load.mockResolvedValue({
            roles: ["Parent"],
            consentRequired: false,
        });
        render(Page);

        expect(
            screen.getByRole("link", { name: "Зарегистрироваться" }),
        ).toHaveAttribute("href", "/register");
        await logIn();
        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith("/parent"),
        );
    });
});
