import { render, screen } from "@testing-library/svelte";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";

import PasswordInput from "./password-input.svelte";

describe("password-input", () => {
    it("скрывает пароль и показывает его по кнопке", async () => {
        const user = userEvent.setup();
        render(PasswordInput, { id: "p", "aria-label": "Пароль" });
        const input = screen.getByLabelText("Пароль");
        await user.type(input, "secret-123");

        expect(input).toHaveAttribute("type", "password");

        await user.click(
            screen.getByRole("button", { name: "Показать пароль" }),
        );
        expect(input).toHaveAttribute("type", "text");
        expect(input).toHaveValue("secret-123");

        await user.click(screen.getByRole("button", { name: "Скрыть пароль" }));
        expect(input).toHaveAttribute("type", "password");
    });

    it("кнопка не отправляет форму", async () => {
        const user = userEvent.setup();
        render(PasswordInput, { "aria-label": "Пароль" });

        expect(
            screen.getByRole("button", { name: "Показать пароль" }),
        ).toHaveAttribute("type", "button");
        await user.click(
            screen.getByRole("button", { name: "Показать пароль" }),
        );
    });
});
