import { isRedirect } from "@sveltejs/kit";
import { describe, expect, it, vi } from "vitest";

import { requireSession } from "./guards";

const session = vi.hoisted(() => ({ load: vi.fn() }));
vi.mock("$lib/session.svelte", () => ({ session }));

async function redirectOf(promise: Promise<unknown>): Promise<string> {
    const thrown = await promise.then(
        () => null,
        (e: unknown) => e,
    );
    if (!isRedirect(thrown)) throw new Error("expected a redirect");
    return thrown.location;
}

const INVITE = new URL("http://localhost/invite/tok-123");

describe("гвард перед страницей приглашения", () => {
    it("без входа уводит на /login и запоминает ссылку", async () => {
        session.load.mockResolvedValue(null);

        expect(await redirectOf(requireSession(INVITE))).toBe(
            "/login?returnUrl=%2Finvite%2Ftok-123",
        );
    });

    it("без согласия на ПДн уводит на /consent и запоминает ссылку", async () => {
        session.load.mockResolvedValue({ roles: [], consentRequired: true });

        expect(await redirectOf(requireSession(INVITE))).toBe(
            "/consent?returnUrl=%2Finvite%2Ftok-123",
        );
    });

    it("вошедшего пускает на страницу, даже без роли родителя", async () => {
        const me = { roles: ["Teacher"], consentRequired: false };
        session.load.mockResolvedValue(me);

        expect(await requireSession(INVITE)).toBe(me);
    });
});
