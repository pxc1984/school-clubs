import { render, screen, waitFor } from "@testing-library/svelte";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import Page from "./+page.svelte";

const navigation = vi.hoisted(() => ({ goto: vi.fn() }));
vi.mock("$app/navigation", () => navigation);

const state = vi.hoisted(() => ({
    page: { url: new URL("http://localhost/register") },
}));
vi.mock("$app/state", () => state);

const auth = vi.hoisted(() => ({
    register: vi.fn(),
    listExternalProviders: vi.fn(),
    externalLoginUrl: vi.fn(() => "/api/v1/auth/external/yandex"),
}));
vi.mock("$lib/api/auth", () => auth);

const session = vi.hoisted(() => ({ load: vi.fn() }));
vi.mock("$lib/session.svelte", () => ({ session }));

const INVITE = "/invite/tok-123";

async function fillAndSubmit() {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText("Фамилия"), "Иванова");
    await user.type(screen.getByLabelText("Имя"), "Мария");
    await user.type(screen.getByLabelText("Email"), "maria@example.com");
    await user.type(screen.getByLabelText("Пароль"), "correct-horse-battery");
    await user.click(screen.getByRole("checkbox"));
    await user.click(
        screen.getByRole("button", { name: "Зарегистрироваться" }),
    );
}

describe("регистрация", () => {
    beforeEach(() => {
        auth.register.mockResolvedValue(undefined);
        auth.listExternalProviders.mockResolvedValue([]);
        session.load.mockResolvedValue({
            roles: ["Parent"],
            consentRequired: false,
        });
    });

    it("по ссылке-приглашению: после регистрации возвращает на ссылку", async () => {
        state.page.url = new URL(
            `http://localhost/register?returnUrl=${encodeURIComponent(INVITE)}`,
        );
        render(Page);

        await fillAndSubmit();

        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith(INVITE),
        );
        expect(auth.register).toHaveBeenCalledWith(
            expect.objectContaining({
                lastName: "Иванова",
                firstName: "Мария",
                email: "maria@example.com",
                acceptPersonalDataPolicy: true,
            }),
        );
        expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute(
            "href",
            `/login?returnUrl=${encodeURIComponent(INVITE)}`,
        );
    });

    it("без ссылки — как раньше, в кабинет родителя", async () => {
        state.page.url = new URL("http://localhost/register");
        render(Page);

        await fillAndSubmit();

        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith("/parent"),
        );
        expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute(
            "href",
            "/login",
        );
    });
});
