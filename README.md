# schoolclubs-dotnet-svelte

Минимальный шаблон: **.NET 10 Web API (Clean Architecture / DDD) + SvelteKit SPA**, PostgreSQL, Docker Compose + Traefik.
Архитектура повторяет `musicclubbot`. Сквозной пример — фича `TodoItems`.

```
backend/     .NET-решение: src/, tests/, Directory.*.props, Dockerfile — см. backend/README.md
frontend/    SvelteKit (adapter-node, Tailwind 4, axios), Dockerfile; Frontend.csproj — обёртка для Rider
traefik/     reverse-proxy: /api, /scalar, /openapi -> backend, остальное -> frontend
SchoolClubs.slnx  открывать в Rider: папки решения /backend и /frontend
```

## Начало работы

```bash
cp -r ~/dev/schoolclubs-dotnet-svelte ~/dev/my-project && cd ~/dev/my-project
./rename.sh MyProject          # SchoolClubs -> MyProject везде
git init
```

## Запуск

```bash
# всё в Docker (traefik на :80)
cp .env.example .env
docker compose up -d --build

# локально (или run-конфиги "db", "Web: http", "dev" в Rider)
docker compose up -d db
dotnet run --project backend/src/Web            # http://localhost:5213/scalar
cd frontend && pnpm install && pnpm dev          # vite проксирует /api на :5213
```

## Проверки

```bash
dotnet test                         # из корня: все тесты бэкенда (интеграционные требуют Docker)
cd frontend && pnpm run check       # svelte-check + tsc + prettier + eslint
```

## Добавить фичу

1. Бэкенд — по шагам в [`backend/README.md`](backend/README.md).
2. Фронт — `frontend/src/lib/api/foo.ts` + страница в `frontend/src/routes/`.

## Frontend.csproj

Пустой проект на `Microsoft.Build.NoTargets`: нужен только чтобы фронтенд был виден в Solution Explorer.
`dotnet build` в нём ничего не делает; собирается фронт через pnpm.
