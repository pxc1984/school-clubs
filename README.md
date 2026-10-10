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
([`deploy.yml`](.github/workflows/deploy.yml)): job меняет тег образов в GitOps-репозитории
[`pxc1984/homelab`](https://github.com/pxc1984/homelab) (`apps/school-clubs-<env>-{backend,frontend}/values.yaml`),
дальше Argo CD сам выкатывает релиз в k3s.

**Prod** — только вручную: Actions → CI/CD → Run workflow (`master`) с галочкой `deploy_prod`
(только `pxc1984`); сначала тот же коммит уходит на dev, потом на prod. Prod-стека в кластере пока нет —
для него нужно завести `apps/school-clubs-prod-*` в homelab и окружение `deploy-prod`.

Окружения GitHub `deploy-dev` и `deploy-prod` (в `deploy-prod` — Deployment branches: только `master`),
в каждом секрет `HOMELAB_DEPLOY_KEY` — приватный ключ deploy key с правом записи в `pxc1984/homelab`.

Конфигурация и секреты (строка подключения, `Auth__*`, ключи S3) — в values.yaml приложений в homelab,
зашифрованы Sealed Secrets. Схема БД мигрирует при старте бэкенда.

Dev — https://sc.iamamaev.ru, k3s, зона grm, namespace `school-clubs-dev`:

| Приложение homelab | Что |
|---|---|
| `school-clubs-dev-backend` | бэкенд; `/api`, `/scalar`, `/openapi`; ключи Data Protection на PVC |
| `school-clubs-dev-frontend` | SPA, всё остальное на хосте |

Файлы (журналы, аватарки) — бакет `school-clubs-dev` в общем S3 кластера (RustFS на grm,
`https://s3.iamamaev.ru`, изнутри — `http://rustfs.s3.svc.cluster.local:9000`; см. `s3/README.md` в homelab).
`Storage__S3__CreateBucket=false`: бакет создан заранее, пользователю S3 доступен только он.

PostgreSQL — база `school_clubs_dev` в `pg-grm` (CloudNativePG на grm). Консоль:
`kubectl -n postgres exec -it pg-grm-1 -- psql school_clubs_dev`, с машины —
`kubectl -n postgres port-forward svc/pg-grm-rw 5432`.

`docker-compose.yml` остаётся для локального запуска.

## Добавить фичу

1. Бэкенд — по шагам в [`backend/README.md`](backend/README.md).
2. Фронт — `frontend/src/lib/api/foo.ts` + страница в `frontend/src/routes/`.

## Frontend.csproj

Пустой проект на `Microsoft.Build.NoTargets`: нужен только чтобы фронтенд был виден в Solution Explorer.
`dotnet build` в нём ничего не делает; собирается фронт через pnpm.
