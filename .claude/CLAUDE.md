# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project: Bookify

## Build & Run

- **Build:** `dotnet build Bookify.sln`
- **Run:** `dotnet run --project Bookify.WebApi`
- **Run all (Docker):** `docker-compose up --build --force-recreate -d`
- **Database Migrations:**
  - Add: `dotnet ef migrations add <Name> --project Bookify.Infrastructure --startup-project Bookify.WebApi`
  - Update: `dotnet ef database update --project Bookify.Infrastructure --startup-project Bookify.WebApi`
- **SDK:** .NET 8.0 (`global.json`, rollForward: `latestMinor`)

Тестовых проектов в решении пока нет.

## Architecture & Structure

**Style:** Clean Architecture + DDD (tactical patterns) + CQRS

**Key Projects/Layers (зависимости направлены внутрь):**
- `Bookify.Domain` — Агрегаты, value objects, доменные события, доменные сервисы (`PricingService`). Ноль внешних зависимостей (только `MediatR.Contracts` для `IDomainEvent`).
- `Bookify.Application` — CQRS: команды (`ICommand`/`ICommandHandler`) и запросы (`IQuery`/`IQueryHandler`). FluentValidation для валидации команд. MediatR pipeline behaviors.
- `Bookify.Infrastructure` — EF Core (PostgreSQL, snake_case), Dapper (read-запросы), репозитории, Keycloak-интеграция (OAuth2/OIDC), Redis-кеширование, Serilog+Seq.
- `Bookify.WebApi` — ASP.NET Core контроллеры (`[ApiController]`, традиционные, не Minimal API), middleware, Swagger.

**Data Flow (Command — запись):**
Controller → `ISender.Send(command)` → MediatR Pipeline (`LoggingBehavior` → `ValidationBehavior` → Handler) → Handler использует репозитории (EF Core) → `IUnitOfWork.SaveChangesAsync()` → `ApplicationDbContext` сохраняет + публикует domain events через `IPublisher` → Domain Event Handlers (email и т.д.)

**Data Flow (Query — чтение):**
Controller → `ISender.Send(query)` → MediatR Pipeline (`LoggingBehavior` → `QueryCachingBehavior` → Handler) → Handler использует `ISqlConnectionFactory` → Dapper raw SQL → DTO напрямую из БД (минуя доменную модель)

## Key Patterns & Technologies

**Patterns:**
- **CQRS** — Команды через EF Core (write model), запросы через Dapper (read model, raw SQL)
- **Result Pattern** — `Result<T>`/`Result` вместо исключений для бизнес-логики. Ошибки — статические поля: `UserErrors.NotFound`, `BookingErrors.Overlap`
- **Repository + Unit of Work** — `Repository<T>` (базовый) + `IUnitOfWork` (= `ApplicationDbContext`)
- **Factory Method** — Сущности создаются через статические методы: `User.Create()`, `Booking.Reserve()`, `Review.Create()`, `DateRange.Create()`. Конструкторы `private`.
- **Domain Events (Observer)** — `IDomainEvent : INotification`. Собираются в `Entity._domainEvents`, публикуются после `SaveChangesAsync` через MediatR `IPublisher`.
- **Decorator** — MediatR Pipeline Behaviors оборачивают каждый handler в цепочку (logging → validation → caching → handler)
- **Singleton** — `Currency.Usd`/`Currency.Eur`, `Role.Registered` — статические экземпляры
- **Optimistic Concurrency** — Row version (`uint Version`) на `Apartment`, `DbUpdateConcurrencyException` → `ConcurrencyException`

**Stack:**
| Библиотека | Версия | Назначение |
|---|---|---|
| .NET / ASP.NET Core | 8.0 | Фреймворк |
| EF Core (Npgsql) | 8.0.11 | ORM, PostgreSQL |
| Dapper | 2.1.66 | Read-запросы (CQRS) |
| MediatR | 13.1.0 | CQRS, domain events |
| FluentValidation | 12.1.0 | Валидация команд |
| Serilog + Seq | 4.3.0 / 9.0.0 | Structured logging |
| StackExchange.Redis | 8.0.23 | Distributed cache |
| Keycloak (JWT Bearer) | 8.0.22 | Auth (OAuth2/OIDC) |
| Swashbuckle | 6.6.2 | Swagger/OpenAPI |

**Validation:** FluentValidation через `ValidationBehavior<,>`. Срабатывает только для `IBaseCommand`. При ошибке бросает `ValidationException` (перехватывается `GlobalExceptionHandler` → 400).

**Logging:** Serilog — Console + Seq sinks. Enrichers: `FromLogContext`, `MachineName`, `ThreadId`. `RequestContextLoggingMiddleware` добавляет `CorrelationId`. `LoggingBehavior` логирует каждый MediatR-запрос.

**Caching:** Redis (cache-aside). Запросы реализуют `ICachedQuery<T>` (свойства `CacheKey`, `Expiration`). `QueryCachingBehavior` проверяет кеш перед вызовом handler. Также кешируются permissions/roles: `auth:permissions-{identityId}`, `auth:roles-{identityId}`.

## Coding Conventions (Strict)

- **Namespaces:** File-scoped (`;`) — 100% кодовой базы
- **Naming:** PascalCase для public-членов, `_camelCase` для private fields (underscore prefix)
- **Async:** Все async-методы оканчиваются на `Async`. `CancellationToken` — всегда последний параметр, `= default` в repository/service, без default в MediatR handlers. Пробрасывается по всей цепочке.
- **DTOs/Records:** `record` для команд, запросов и value objects. `sealed class` с `{ get; init; }` для response DTO.
- **Nullability:** `<Nullable>enable</Nullable>` во всех проектах. Проверки `is null` / `is not null`.
- **ImplicitUsings:** `<ImplicitUsings>enable</ImplicitUsings>` во всех проектах
- **Sealed:** ~90% конкретных классов — `sealed` (entities, handlers, repositories, configurations, services, DTOs)
- **Access modifiers:** Всегда явные. Handlers и repositories — `internal sealed`. Controllers — `public`. Domain entities — `public sealed`.
- **var:** Используется для очевидных типов (`var user = await _repo.GetByIdAsync(...)`) и LINQ. Explicit types для `Result<T>` и при неочевидности.
- **Raw string literals** (`"""..."""`) для SQL-запросов в Dapper
- **Collection expressions** (`[item1, item2]`) для массивов
- **Pattern matching:** `is not null`, `switch` expressions, property patterns (`is not { IsAuthenticated: true }`)
- **Primary constructors** (C# 12) в некоторых классах (напр. `GlobalExceptionHandler`)

## Testing Strategy

Тестовых проектов в решении нет. Solution file содержит пустую папку `test`.

## Auth & Authorization

Keycloak (OAuth2/OIDC) как внешний identity provider. JWT Bearer аутентификация.

**Permission-based авторизация:**
- `[HasPermission("permission:name")]` — custom атрибут на эндпоинтах
- Цепочка: User → Role → Permission (junction-таблицы в БД)
- `CustomClaimsTransformation` загружает роли по `IdentityId` из БД, добавляет `ClaimTypes.Role` в claims
- `PermissionAuthorizationHandler` + `PermissionAuthorizationPolicyProvider` — динамические политики
- Результаты авторизации кешируются в Redis

**Регистрация пользователя:** Domain `User.Create()` → `IAuthenticationService.RegisterAsync()` (Keycloak Admin API) → получение `IdentityId` из Location header → сохранение в БД.

## Infrastructure

**Docker Compose сервисы:**
| Сервис | Порт | Назначение |
|---|---|---|
| bookify-db | 5435:5432 | PostgreSQL |
| bookify-idp | 18080:8080 | Keycloak |
| bookify-seq | 8081:80, 5341 | Seq (логи) |
| bookify-redis | 6379 | Redis |
| bookify-webapi | 5001:8080 | API |

Переменные окружения из `.env` файла. Realm Keycloak: `.files/bookify-realm-export.json`.

**DI-регистрация:** Каждый слой имеет `DependencyInjection.cs`: `AddApplication()`, `AddInfrastructure(IConfiguration)`. Вызов в `Program.cs`.

**Lifetimes:** `ApplicationDbContext` / Repositories / `IUnitOfWork` / `IUserContext` — Scoped. `ISqlConnectionFactory` / `ICacheService` — Singleton. `IDateTimeProvider` / `IEmailService` / `PricingService` — Transient.

**Middleware Pipeline (порядок в Program.cs):**
1. `UseRequestContextLogging()` — CorrelationId в Serilog context
2. `UseSerilogRequestLogging()` — HTTP request logging
3. `UseExceptionHandler()` — `GlobalExceptionHandler` (RFC 7807 Problem Details)
4. `UseAuthentication()` → `UseAuthorization()`
5. `MapControllers()`

## Critical Implementation Details

- **Domain entities НИКОГДА не возвращаются в API.** Контроллеры возвращают DTO (`BookingResponseDto`, `ApartmentResponseDto` и т.д.). Query handlers через Dapper маппят SQL прямо в DTO.
- **Result → HTTP маппинг ручной.** Нет автоматической конвертации. Контроллер проверяет `result.IsSuccess` и вручную возвращает `Ok()` / `BadRequest()` / `NotFound()` / `CreatedAtAction()`.
- **`ISqlConnectionFactory.CreateConnection()` открывает соединение синхронно.** Callers обязаны `using var connection = ...`. Зарегистрирован как Singleton.
- **Оптимистичная конкурентность на Apartment** — `Property<uint>("Version").IsRowVersion()` (PostgreSQL `xmin`). `DbUpdateConcurrencyException` → `ConcurrencyException` → в handler ловится как `BookingErrors.Overlap` (не как 409).
- **Domain events публикуются ПОСЛЕ `SaveChangesAsync`** — в `ApplicationDbContext` переопределён `SaveChangesAsync`: сначала persist, затем `PublishDomainEventsAsync()` через MediatR. Нет outbox pattern.
- **`GlobalExceptionHandler`:** `ValidationException` → 400 (errors в extensions), `ConcurrencyException` → 409, всё остальное → 500 (детали скрыты: "An internal server error has occurred").
- **FK без navigation properties между агрегатами.** `Booking → Apartment` и `Booking → User` настроены через `.HasOne<T>().WithMany().HasForeignKey(...)` без навигационных свойств — соблюдение границ агрегатов.
- **Value objects в EF Core:** `Money` — `OwnsOne` (owned entity, 2 колонки: amount + currency). `Currency` — `HasConversion` (string code ↔ object). Простые VOs (`FirstName`, `Email`) — `HasConversion(vo => vo.Value, v => new VO(v))`.
- **`DateOnlyTypeHandler`** — кастомный Dapper `SqlMapper.TypeHandler<DateOnly>` для корректной работы с PostgreSQL `date`.
- **`IDateTimeProvider`** вместо `DateTime.UtcNow` — для testability. Инжектится в handlers.
- **Порядок MediatR behaviors критичен:** `LoggingBehavior` → `ValidationBehavior` (только `IBaseCommand`) → `QueryCachingBehavior` (только `ICachedQuery`). Регистрация в `AddApplication()`.
