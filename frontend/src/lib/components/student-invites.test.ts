import { render, screen, waitFor } from "@testing-library/svelte";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { ChildInviteDto } from "$lib/api/admin";
import StudentInvites from "./student-invites.svelte";

const api = vi.hoisted(() => ({
    listStudentInvites: vi.fn(),
    createStudentInvite: vi.fn(),
    revokeStudentInvite: vi.fn(),
}));
vi.mock("$lib/api/admin", async (importOriginal) => ({
    ...(await importOriginal<typeof import("$lib/api/admin")>()),
    ...api,
}));

const toast = vi.hoisted(() => ({ success: vi.fn(), error: vi.fn() }));
vi.mock("svelte-sonner", () => ({ toast }));

const STUDENT_ID = "11111111-1111-1111-1111-111111111111";

function invite(token: string): ChildInviteDto {
    return {
        id: `id-${token}`,
        token,
        created: "2026-10-10T12:00:00Z",
        expiresAt: "2026-11-09T12:00:00Z",
    };
}

function linkOf(token: string): string {
    return `${window.location.origin}/invite/${token}`;
}

describe("student-invites", () => {
    beforeEach(() => {
        api.listStudentInvites.mockResolvedValue([]);
    });

    it("показывает действующие ссылки ученика", async () => {
        api.listStudentInvites.mockResolvedValue([
            invite("aaa"),
            invite("bbb"),
        ]);

        render(StudentInvites, { studentId: STUDENT_ID });

        expect(await screen.findByDisplayValue(linkOf("aaa"))).toBeVisible();
        expect(screen.getByDisplayValue(linkOf("bbb"))).toBeVisible();
        expect(api.listStudentInvites).toHaveBeenCalledWith(STUDENT_ID);
    });

    it("создаёт ссылку и сразу копирует её в буфер", async () => {
        const user = userEvent.setup();
        api.createStudentInvite.mockResolvedValue(invite("new"));
        render(StudentInvites, { studentId: STUDENT_ID });

        await user.click(
            screen.getByRole("button", {
                name: "Создать и скопировать ссылку",
            }),
        );

        expect(api.createStudentInvite).toHaveBeenCalledWith(STUDENT_ID);
        await waitFor(() =>
            expect(toast.success).toHaveBeenCalledWith(
                "Ссылка скопирована",
                expect.anything(),
            ),
        );
        expect(await navigator.clipboard.readText()).toBe(linkOf("new"));
        expect(screen.getByDisplayValue(linkOf("new"))).toBeVisible();
    });

    it("копирует уже созданную ссылку повторно", async () => {
        const user = userEvent.setup();
        api.listStudentInvites.mockResolvedValue([invite("aaa")]);
        render(StudentInvites, { studentId: STUDENT_ID });

        await user.click(
            await screen.findByRole("button", { name: "Скопировать ссылку" }),
        );

        await waitFor(() => expect(toast.success).toHaveBeenCalled());
        expect(await navigator.clipboard.readText()).toBe(linkOf("aaa"));
        expect(api.createStudentInvite).not.toHaveBeenCalled();
    });

    it("если буфер недоступен — просит скопировать вручную, ссылка остаётся на экране", async () => {
        const user = userEvent.setup();
        vi.spyOn(navigator.clipboard, "writeText").mockRejectedValue(
            new Error("denied"),
        );
        api.createStudentInvite.mockResolvedValue(invite("new"));
        render(StudentInvites, { studentId: STUDENT_ID });

        await user.click(
            screen.getByRole("button", {
                name: "Создать и скопировать ссылку",
            }),
        );

        await waitFor(() =>
            expect(toast.error).toHaveBeenCalledWith(
                "Не удалось скопировать — выделите ссылку вручную",
            ),
        );
        expect(screen.getByDisplayValue(linkOf("new"))).toBeVisible();
    });

    it("отзывает ссылку и убирает её из списка", async () => {
        const user = userEvent.setup();
        api.listStudentInvites.mockResolvedValue([
            invite("aaa"),
            invite("bbb"),
        ]);
        api.revokeStudentInvite.mockResolvedValue(undefined);
        render(StudentInvites, { studentId: STUDENT_ID });

        const [first] = await screen.findAllByRole("button", {
            name: "Отозвать ссылку",
        });
        await user.click(first);

        expect(api.revokeStudentInvite).toHaveBeenCalledWith(
            STUDENT_ID,
            "id-aaa",
        );
        await waitFor(() =>
            expect(
                screen.queryByDisplayValue(linkOf("aaa")),
            ).not.toBeInTheDocument(),
        );
        expect(screen.getByDisplayValue(linkOf("bbb"))).toBeVisible();
    });
});
