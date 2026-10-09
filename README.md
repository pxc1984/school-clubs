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

## Деплой

CI/CD — [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml). На каждый PR и push в `master` — тесты
бэкенда и проверки фронта. На push в `master` дополнительно собираются образы
`ghcr.io/pxc1984/school-clubs-{backend,frontend}:<sha>` и выкатываются на **dev**
([`deploy.yml`](.github/workflows/deploy.yml)): по SSH на сервер копируются только `docker-compose.yml`,
`docker-compose.prod.yml` и `traefik/`, образы скачиваются из GHCR — исходников на сервере нет.

**Prod** — только вручную: Actions → CI/CD → Run workflow (`master`) с галочкой `deploy_prod`
(только `pxc1984`); сначала тот же коммит уходит на dev, потом на prod.

Окружения GitHub `deploy-dev` и `deploy-prod` (в `deploy-prod` — Deployment branches: только `master`),
в каждом секреты:

| Секрет | Что |
|---|---|
| `SSH_KEY` | приватный ed25519-ключ пользователя деплоя |
| `HOST`, `USERNAME` | сервер и пользователь (в группе `docker`) |
| `DEPLOY_PATH` | каталог на сервере, например `/opt/school-clubs-dev`; имя каталога = имя compose-проекта |
| `ENV` | содержимое `.env` по образцу [`.env.example`](.env.example): свои пароли PostgreSQL/S3, `Auth__*` |

Dev и prod могут жить на одном сервере в разных `DEPLOY_PATH` — тогда в `ENV` dev задайте другой
`HTTP_PORT`. На сервере наружу ничего не публикуется: traefik слушает `127.0.0.1:$HTTP_PORT`, туда проксирует
хостовой nginx (он же терминирует HTTPS), поэтому в `ENV` — `ForwardedHeaders__ForwardLimit=2`. nginx должен
передавать `Host`, `X-Forwarded-For` и `X-Forwarded-Proto`. Схема БД мигрирует при старте бэкенда.

Dev: `pxc1984/school-clubs` → окружение `deploy-dev`, `school218clubs@109.120.137.209:/home/school218clubs/school-clubs-dev`,
порт 52140 (https://sc.iamamaev.ru), PostgreSQL для диагностики — `109.120.137.209:59977` (`DB_BIND`/`DB_PORT` в `ENV`).

## Добавить фичу

1. Бэкенд — по шагам в [`backend/README.md`](backend/README.md).
2. Фронт — `frontend/src/lib/api/foo.ts` + страница в `frontend/src/routes/`.

## Frontend.csproj

Пустой проект на `Microsoft.Build.NoTargets`: нужен только чтобы фронтенд был виден в Solution Explorer.
`dotnet build` в нём ничего не делает; собирается фронт через pnpm.
