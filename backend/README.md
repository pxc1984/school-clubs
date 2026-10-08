# Backend

.NET 10 Web API, Clean Architecture / DDD. Зависимости: `Web → Infrastructure → Application → Domain` (+ `Shared`).

```
src/
├── Domain/          Entities/, Abstractions/ (IRepository<T>, IUnitOfWork, I{Entity}Repository), Common/
├── Application/     Services/<Feature>/ (I{Feature}Service, сервис, DTO), Validators/, Common/Exceptions/
├── Infrastructure/  Data/ (DbContext, Configurations/, Repositories/, Interceptors/, Initialiser)
├── Shared/          Services.cs (имя строки подключения)
└── Web/             Program.cs, Endpoints/v1/<Feature>/, Infrastructure/ (ProblemDetails, MapEndpoints)
tests/
├── Domain.UnitTests/                 NUnit + Shouldly
├── Application.UnitTests/            + Moq
└── Infrastructure.IntegrationTests/  WebApplicationFactory + Testcontainers PostgreSQL + Respawn
```

Пакеты — централизованно в `Directory.Packages.props`; сборочные артефакты — в `backend/artifacts/`.

## Запуск

```bash
docker compose up -d db                  # из корня репозитория
dotnet run --project backend/src/Web     # http://localhost:5213/scalar, health — /health
```

## Добавить фичу

1. `Domain/Entities/Foo.cs` (+ `IAuditableEntity` при необходимости), `Domain/Abstractions/IFooRepository.cs`.
2. `Infrastructure/Data/Configurations/FooConfiguration.cs` (snake_case таблица/колонки),
   `Repositories/FooRepository.cs`, регистрация в `Infrastructure/DependencyInjection.cs`.
3. `Application/Services/Foo/` — интерфейс, сервис, DTO; валидаторы в `Validators/`; регистрация в
   `Application/DependencyInjection.cs`.
4. `Web/Endpoints/v1/Foo/Foo.cs` + строка в `WebApplicationExtensions.MapEndpoints`.
5. Тесты: юнит на сервис с моком репозитория, интеграционный на эндпоинт (наследник `TestBase`).

## Миграции

Пока миграций нет, схема создаётся `EnsureCreated`; как только появится первая — инициализатор применяет `Migrate`.

```bash
dotnet ef migrations add Initial --project backend/src/Infrastructure --startup-project backend/src/Web --output-dir Data/Migrations
```

> Если БД уже создана через `EnsureCreated`, перед первой миграцией её нужно пересоздать (`docker compose down -v`).
