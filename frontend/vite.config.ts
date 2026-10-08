import { defineConfig } from "vite";
import { sveltekit } from "@sveltejs/kit/vite";
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
    envPrefix: ["VITE_", "API_"],
    plugins: [sveltekit(), tailwindcss()],
    server: {
        proxy: {
            // в docker-compose /api маршрутизирует traefik; локально — vite
            "/api": {
                target: process.env.API_SSR_URL || "http://localhost:5213",
                changeOrigin: true,
            },
        },
    },
});
