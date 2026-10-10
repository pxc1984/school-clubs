import { defineConfig } from "vitest/config";
import { sveltekit } from "@sveltejs/kit/vite";
import { svelteTesting } from "@testing-library/svelte/vite";
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
    envPrefix: ["VITE_", "API_"],
    plugins: [sveltekit(), tailwindcss(), svelteTesting()],
    server: {
        proxy: {
            // в docker-compose /api маршрутизирует traefik; локально — vite
            "/api": {
                target: process.env.API_SSR_URL || "http://localhost:5213",
                changeOrigin: true,
            },
        },
    },
    test: {
        environment: "jsdom",
        include: ["src/**/*.test.ts"],
        setupFiles: ["src/tests/setup.ts"],
        restoreMocks: true,
    },
});
