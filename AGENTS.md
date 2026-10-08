# AGENTS.md

.NET 10 Web API (Clean Architecture / DDD) в `backend/` + SvelteKit SPA в `frontend/`, PostgreSQL, Docker Compose + Traefik.
См. `README.md` (запуск) и `backend/README.md` (добавление фичи); сквозной пример — `TodoItems`.
Решение `SchoolClubs.slnx` в корне; `frontend/Frontend.csproj` — пустая NoTargets-обёртка только для Solution Explorer.

## Слои (`backend/src/`)

Зависимости: `Web → Infrastructure → Application → Domain` (+ `Shared`).

- **Domain** — сущности (`Entities/`), интерфейсы репозиториев и `IUnitOfWork` (`Abstractions/`), `Common/IAuditableEntity`.
  Никаких зависимостей от EF Core.
- **Application** — бизнес-логика в `Services/<Feature>/` (интерфейс + реализация + DTO-рекорды),
  FluentValidation-валидаторы в `Validators/` (сканируются из сборки), исключения в `Common/Exceptions/`.
  Сервисы зависят от абстракций репозиториев, не от `DbContext`.
- **Infrastructure** — `ApplicationDbContext`, Fluent API-конфигурации (`Data/Configurations/`, таблицы и колонки
  в snake_case, единственное число), `Repository<T>` + специфичные репозитории, `UnitOfWork`,
  `AuditableEntityInterceptor`, `ApplicationDbContextInitialiser` (EnsureCreated → Migrate, seed).
- **Web** — Minimal API: статический класс на группу в `Endpoints/v1/<Feature>/` с `Map(RouteGroupBuilder)`,
  регистрация в `Infrastructure/WebApplicationExtensions.cs`. Исключения → ProblemDetails
  (`ValidationException` 400, `NotFoundException` из Ardalis.GuardClauses 404, `UnauthorizedAccessException` 401,
  `ForbiddenAccessException` 403). OpenAPI + Scalar на `/scalar`, health на `/health`.

DI: `AddApplicationServices` / `AddInfrastructureServices` / `AddWebServices` (по `DependencyInjection.cs` в каждом
проекте). Базовый `DbContext` маппится на `ApplicationDbContext`; `IRepository<>` зарегистрирован open-generic.

## Frontend (`frontend/`)

SvelteKit + adapter-node, Svelte 5 runes, Tailwind 4, axios-клиент в `$lib/api/client.ts`, по файлу API на фичу.
Запросы идут на same-origin `/api` (в Docker — traefik, локально — vite proxy на `API_SSR_URL`, по умолч. `:5213`).
pnpm; `pnpm run check` перед коммитом.

## Конвенции

- Пакеты — централизованно в `backend/Directory.Packages.props`; общие свойства — `backend/Directory.Build.props`.
- Стиль C# — `.editorconfig`: 4 пробела, без expression-bodied членов, `var`, однострочные if без скобок допустимы.
- Пространства имён `SchoolClubs.{Layer}...`; DTO — суффикс `Dto`/`Request`/`Response`.
- Тесты: NUnit + Shouldly (+ Moq в Application); интеграционные — `WebApplicationFactory<Program>` +
  Testcontainers PostgreSQL + Respawn (`TestBase` сбрасывает БД перед каждым тестом).
