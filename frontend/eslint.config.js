import js from "@eslint/js";
import globals from "globals";
import svelte from "eslint-plugin-svelte";
import tseslint from "typescript-eslint";

export default tseslint.config(
    js.configs.recommended,
    ...tseslint.configs.recommended,
    ...svelte.configs.recommended,
    {
        languageOptions: {
            globals: {
                ...globals.browser,
            },
        },
    },
    {
        files: ["**/*.svelte", "**/*.svelte.ts"],
        languageOptions: {
            parserOptions: {
                parser: tseslint.parser,
            },
        },
    },
    {
        rules: {
            // правило нужно для приложений с paths.base; SPA живёт в корне, resolve() только шумит
            "svelte/no-navigation-without-resolve": "off",
        },
    },
    {
        ignores: [
            ".svelte-kit/**",
            "dist/**",
            "build/**",
            "node_modules/**",
            "bin/**",
            "obj/**",
        ],
    },
);
