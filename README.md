# school-clubs

Электронный журнал для школьных кружков: преподаватели отмечают посещаемость, родители видят занятия
и отметки своих детей, администратор ведёт группы, учеников и пользователей.

**Стек:** .NET 10 Web API · SvelteKit · PostgreSQL · S3 · Docker Compose + Traefik

```
backend/    .NET-решение (Clean Architecture) — см. backend/README.md
frontend/   SvelteKit SPA
traefik/    /api, /scalar, /openapi → backend, остальное → frontend
```

## Запуск

```bash
cp .env.example .env
docker compose up -d --build                  # всё целиком, http://localhost
```

Для разработки:

```bash
docker compose up -d db
dotnet run --project backend/src/Web          # http://localhost:5213/scalar
cd frontend && pnpm install && pnpm dev
```

## Проверки

```bash
dotnet test                                   # интеграционные тесты требуют Docker
cd frontend && pnpm run check && pnpm test
```

## Деплой

Push в `master` → тесты → образы в `ghcr.io/pxc1984/school-clubs-*` → тег обновляется в
[`pxc1984/homelab`](https://github.com/pxc1984/homelab) → Argo CD выкатывает на dev:
https://sc.iamamaev.ru. Prod — вручную, workflow CI/CD с флагом `deploy_prod`.
