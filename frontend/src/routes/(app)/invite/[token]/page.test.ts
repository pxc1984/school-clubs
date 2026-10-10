import { render, screen, waitFor } from "@testing-library/svelte";
import { AxiosError, type AxiosResponse } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { AcceptedChildInviteDto } from "$lib/api/parent";
import Page from "./+page.svelte";

const navigation = vi.hoisted(() => ({ goto: vi.fn() }));
vi.mock("$app/navigation", () => navigation);

vi.mock("$app/state", () => ({
    page: { params: { token: "tok-123" } },
}));

const parentApi = vi.hoisted(() => ({ acceptChildInvite: vi.fn() }));
vi.mock("$lib/api/parent", () => parentApi);

const session = vi.hoisted(() => ({
    load: vi.fn(),
    homeHref: vi.fn(() => "/parent"),
}));
vi.mock("$lib/session.svelte", () => ({ session }));

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock("svelte-sonner", () => ({ toast }));

const STUDENT_ID = "22222222-2222-2222-2222-222222222222";

function accepted(alreadyLinked: boolean): AcceptedChildInviteDto {
    return {
        student: {
            id: STUDENT_ID,
            lastName: "Иванов",
            firstName: "Пётр",
            middleName: "",
            grade: "7Б",
        },
        alreadyLinked,
    };
}

function httpError(status: number): AxiosError {
    return new AxiosError("failed", "ERR", undefined, undefined, {
        status,
        data: {},
    } as AxiosResponse);
}

describe("страница /invite/[token]", () => {
    beforeEach(() => {
        session.load.mockResolvedValue(null);
    });

    it("привязывает ребёнка и открывает его страницу", async () => {
        parentApi.acceptChildInvite.mockResolvedValue(accepted(false));

        render(Page);

        expect(screen.getByText("Добавляем ребёнка…")).toBeVisible();
        await waitFor(() =>
            expect(navigation.goto).toHaveBeenCalledWith(
                `/parent/children/${STUDENT_ID}`,
                { replaceState: true },
            ),
        );
        expect(parentApi.acceptChildInvite).toHaveBeenCalledWith("tok-123");
        // роль «Родитель» могла появиться только что — сессию перечитываем до перехода
        expect(session.load).toHaveBeenCalledWith(true);
        expect(session.load.mock.invocationCallOrder[0]).toBeLessThan(
            navigation.goto.mock.invocationCallOrder[0],
        );
        expect(toast.success).toHaveBeenCalledWith(
            expect.stringContaining("добавлен"),
        );
    });

    it("повторный переход по своей ссылке — не ошибка", async () => {
        parentApi.acceptChildInvite.mockResolvedValue(accepted(true));

        render(Page);

        await waitFor(() => expect(navigation.goto).toHaveBeenCalled());
        expect(toast.success).toHaveBeenCalledWith(
            expect.stringContaining("уже в вашем кабинете"),
        );
    });

    it.each([410, 404])(
        "недействительная ссылка (%i) — просит новую у администратора",
        async (status) => {
            parentApi.acceptChildInvite.mockRejectedValue(httpError(status));

            render(Page);

            expect(
                await screen.findByText("Не удалось добавить ребёнка"),
            ).toBeVisible();
            expect(
                screen.getByText(/Попросите у администратора кружков новую/),
            ).toBeVisible();
            expect(
                screen.getByRole("link", { name: "В кабинет" }),
            ).toHaveAttribute("href", "/parent");
            expect(navigation.goto).not.toHaveBeenCalled();
        },
    );

    it("сервисный админ без аккаунта (403) — просит войти родителем", async () => {
        parentApi.acceptChildInvite.mockRejectedValue(httpError(403));

        render(Page);

        expect(
            await screen.findByText(/Войдите под аккаунтом родителя/),
        ).toBeVisible();
    });

    it("слишком много попыток (429) — просит подождать", async () => {
        parentApi.acceptChildInvite.mockRejectedValue(httpError(429));

        render(Page);

        expect(await screen.findByText(/Слишком много попыток/)).toBeVisible();
        expect(navigation.goto).not.toHaveBeenCalled();
    });
});
