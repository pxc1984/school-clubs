import { render, screen } from "@testing-library/svelte";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import type { ChildLessonDto, ChildSummaryDto } from "$lib/api/parent";
import Page from "./+page.svelte";

vi.mock("$app/navigation", () => ({ goto: vi.fn() }));
vi.mock("$app/state", () => ({
    page: { url: new URL("http://localhost/parent/children/s1") },
}));

const student = {
    id: "s1",
    lastName: "Смирнова",
    firstName: "Анна",
    middleName: "",
    grade: "7А",
};
const group = { id: "g1", title: "Олимпиадная математика", isArchived: false };

function lesson(overrides: Partial<ChildLessonDto>): ChildLessonDto {
    return {
        lessonId: crypto.randomUUID(),
        date: "2026-09-24",
        time: null,
        topic: "Графы",
        attended: true,
        attendance: null,
        solved: 4,
        taskCount: 5,
        score: 0.8,
        groupAverageSolved: 3.1,
        tasks: [],
        ...overrides,
    };
}

function renderPage(
    summary: Partial<ChildSummaryDto>,
    lessons: ChildLessonDto[],
) {
    const data = {
        child: { ...student, groups: [group] },
        group,
        periodKey: "quarter",
        summary: {
            student,
            group,
            from: null,
            to: null,
            lessonsAttended: 3,
            lessonsTotal: 4,
            tasksSolved: 12,
            tasksTotal: 15,
            score: 0.8,
            position: null,
            perfectStreak: null,
            chart: [],
            ...summary,
        },
        lessons,
    };
    render(Page, { data: data as never });
}

describe("страница ребёнка", () => {
    it("тексты не предполагают пол ребёнка", async () => {
        renderPage({ perfectStreak: 3 }, [
            lesson({ topic: "Графы", attendance: "Late" }),
            lesson({
                topic: "Чётность",
                attended: false,
                solved: 0,
                score: null,
            }),
        ]);

        expect(screen.getByText("3 из 4 занятий")).toBeVisible();
        expect(screen.getByText(/Всё решено 3 занятия подряд/)).toBeVisible();
        expect(screen.getByText("опоздание")).toBeVisible();
        expect(screen.getByText("пропуск")).toBeVisible();
        // и внутри раскрытых занятий
        const user = userEvent.setup();
        for (const trigger of screen.getAllByRole("button", {
            expanded: false,
        }))
            await user.click(trigger);
        expect(screen.getByText("Опоздание на занятие.")).toBeVisible();
        expect(screen.getByText("Занятие пропущено")).toBeVisible();
        const text = document.body.textContent ?? "";
        expect(text).not.toMatch(
            /(?<!\p{L})(Был|был|Решил|решил|опоздал)(?!\p{L})/u,
        );
    });

    it("объясняет цвета задач легендой", () => {
        renderPage({}, [lesson({})]);

        const legend = screen.getByRole("list", { name: "Цвета задач" });
        expect(legend).toHaveTextContent("Решено");
        expect(legend).toHaveTextContent("Не решено, есть продвижение");
    });

    it("дата занятия — коротко, в одну строку", () => {
        renderPage({}, [lesson({ date: "2026-09-24" })]);

        expect(screen.getByText("24 сент.")).toBeVisible();
    });
});
