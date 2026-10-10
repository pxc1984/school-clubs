import { render, screen } from "@testing-library/svelte";
import { describe, expect, it } from "vitest";

import type { TeacherGroupCardDto } from "$lib/api/teacher";
import Page from "./+page.svelte";

function card(overrides: Partial<TeacherGroupCardDto>): TeacherGroupCardDto {
    return {
        id: "g1",
        title: "Олимпиадная математика",
        description: null,
        studentCount: 12,
        lastLessonDate: "2026-10-08",
        nextLessonDate: null,
        unfilledLessonCount: 0,
        unpublishedLessonCount: 0,
        ...overrides,
    };
}

function renderPage(groups: TeacherGroupCardDto[]) {
    render(Page, { data: { groups } as never });
}

describe("главная учителя", () => {
    it("напоминает о заполненных, но не опубликованных журналах", () => {
        renderPage([
            card({ unpublishedLessonCount: 2, unfilledLessonCount: 1 }),
        ]);

        expect(screen.getByText("2 журнала не опубликованы")).toBeVisible();
        expect(screen.getByText("1 журнал не заполнен")).toBeVisible();
    });

    it("без долгов — без напоминаний", () => {
        renderPage([card({})]);

        expect(screen.queryByText(/не опубликован/)).not.toBeInTheDocument();
        expect(screen.queryByText(/не заполнен/)).not.toBeInTheDocument();
    });
});
