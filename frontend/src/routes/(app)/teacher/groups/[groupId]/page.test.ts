import { render, screen, within } from "@testing-library/svelte";
import { describe, expect, it, vi } from "vitest";

import type { LessonSummaryDto } from "$lib/api/teacher";
import Page from "./+page.svelte";

vi.mock("$app/navigation", () => ({ goto: vi.fn(), invalidateAll: vi.fn() }));

function lesson(overrides: Partial<LessonSummaryDto>): LessonSummaryDto {
    return {
        id: crypto.randomUUID(),
        date: "2026-09-24",
        time: "16:00:00",
        topic: "Графы",
        taskCount: 5,
        filledPercent: 100,
        isPublished: true,
        ...overrides,
    };
}

function renderPage(lessons: LessonSummaryDto[]) {
    const group = {
        id: "g1",
        title: "Олимпиадная математика",
        description: null,
        isArchived: false,
        students: [],
        lessons,
    };
    render(Page, {
        data: { tab: "lessons", group, lessons, summary: null } as never,
    });
}

function rowOf(topic: string): HTMLElement {
    return screen.getAllByText(topic)[0].closest("tr")!;
}

describe("занятия группы", () => {
    it("различает опубликованный, заполненный неопубликованный и черновик", () => {
        renderPage([
            lesson({ topic: "Графы" }),
            lesson({ topic: "Делимость", isPublished: false }),
            lesson({
                topic: "Инварианты",
                isPublished: false,
                filledPercent: 40,
            }),
        ]);

        expect(within(rowOf("Графы")).getByText("Опубликован")).toBeVisible();
        expect(
            within(rowOf("Делимость")).getByText("Не опубликован"),
        ).toBeVisible();
        expect(within(rowOf("Инварианты")).getByText("Черновик")).toBeVisible();
    });

    it("будущее занятие — «Запланировано», даже с пустым журналом", () => {
        renderPage([
            lesson({
                topic: "Комбинаторика",
                date: "2999-01-01",
                isPublished: false,
                filledPercent: 0,
            }),
        ]);

        expect(
            within(rowOf("Комбинаторика")).getByText("Запланировано"),
        ).toBeVisible();
    });
});
