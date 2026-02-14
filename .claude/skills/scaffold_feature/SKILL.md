---
name: scaffold-feature
description: Scaffolds a complete vertical feature slice across all layers of Clean Architecture — from Domain to API. Creates entity (if new), commands, queries, DTOs, validators, EF configuration, repository, controller, domain events, and DI registration in one go. Use when asked to implement a complete feature, add a full CRUD, scaffold a new module, build end-to-end functionality, or create everything needed for a new business concept (e.g. "implement reviews feature", "add full CRUD for payments", "create the notification system").
---

# Full Feature Vertical Slice Scaffolder

You are a senior .NET backend developer specializing in Clean Architecture + DDD + CQRS. Your task is to scaffold a complete feature across all architectural layers in the Bookify project.

## Process

### Step 1: Requirements Discovery

This is a large task. Thoroughly clarify ALL requirements before generating ANY code:

1. **Feature name** and business context
2. **Entity design:**
   - Properties and their types
   - Value Objects needed
   - Status/state machine (if any)
   - Factory method semantics (Create, Reserve, Submit, Place...)
   - Relationships to existing aggregates (Apartment, User, Booking)
3. **Write operations (Commands):**
   - What actions can users perform? (create, update, delete, state transitions)
   - What validation rules apply?
   - What domain events should be raised?
4. **Read operations (Queries):**
   - What data retrieval is needed? (get by ID, search, list, filter)
   - Should any queries be cached?
   - Resource-based authorization (filter by current user)?
5. **API design:**
   - Route prefix (e.g., `api/reviews`)
   - Which endpoints need authorization?
   - HTTP methods and response codes

### Step 2: Plan the File Structure

Present the plan to the user BEFORE generating code. Example plan:

```
Bookify.Domain/Reviews/
├── Review.cs                           (aggregate root)
├── IReviewRepository.cs                (repository interface)
├── ValueObjects/
│   ├── Rating.cs
│   └── Comment.cs
├── Enums/
│   └── ReviewStatus.cs                 (if stateful)
├── Events/
│   └── ReviewCreatedDomainEvent.cs
└── Validation/
    └── ReviewErrors.cs

Bookify.Application/Reviews/
├── Commands/
│   └── CreateReview/
│       ├── CreateReviewCommand.cs
│       ├── CreateReviewCommandHandler.cs
│       └── ReviewCreatedDomainEventHandler.cs
├── Queries/
│   └── GetReview/
│       ├── GetReviewQuery.cs
│       └── GetReviewQueryHandler.cs
├── Validation/
│   └── CreateReviewCommandValidator.cs
└── Dtos/
    ├── CreateReviewRequestDto.cs
    └── ReviewResponseDto.cs

Bookify.Infrastructure/
├── Configurations/
│   └── ReviewConfiguration.cs
└── Repositories/
    └── ReviewRepository.cs

Bookify.WebApi/Controllers/Reviews/
└── ReviewController.cs
```

### Step 3: Generate Code Layer by Layer

Follow this strict order to avoid compilation errors:

#### Phase 1: Domain Layer (no dependencies)
1. Enums
2. Value Objects
3. Domain Events
4. Error definitions
5. Entity (aggregate root)
6. Repository interface

#### Phase 2: Application Layer
7. Response DTOs
8. Request DTOs
9. Command records
10. Query records (with ICachedQuery if needed)
11. Command Handlers
12. Query Handlers (with Dapper SQL)
13. Validators (FluentValidation)
14. Domain Event Handlers

#### Phase 3: Infrastructure Layer
15. EF Core Configuration
16. Repository Implementation
17. DI Registration (update `DependencyInjection.cs`)

#### Phase 4: Web API Layer
18. Controller with all endpoints

### Step 4: Follow Conventions Strictly

For EACH file, apply the correct conventions from the project:

<conventions>
**Naming & Structure:**
- File-scoped namespaces (`;`) — 100% of the codebase
- PascalCase for public members, `_camelCase` for private fields
- `sealed` on ~90% of concrete classes
- Handlers and repositories: `internal sealed`
- Controllers: `public`
- Domain entities: `public sealed`

**Async:**
- All async methods end with `Async`
- `CancellationToken` always last parameter
- `= default` in repository/service, NO default in MediatR handlers
- Pass CancellationToken through ALL async chains

**Types:**
- `record` for commands, queries, value objects, domain events
- `sealed class` with `{ get; init; }` for response DTOs
- `sealed record` for request DTOs (positional parameters)
- `var` for obvious types, explicit for `Result<T>`

**Domain:**
- Private constructors + static factory methods on entities
- Properties: `{ get; private set; }`
- Result pattern (no exceptions for business logic)
- `RaiseDomainEvent()` after state changes
- FKs as Guid — no navigation properties between aggregates
- `DateTime utcNow` parameter (never `DateTime.UtcNow` directly)

**Infrastructure:**
- EF Config: `builder.ToTable("snake_case_plural")`
- Money: `OwnsOne` with Currency `HasConversion`
- Simple VOs: `HasConversion(vo => vo.Value, v => new VO(v))`
- FKs: `.HasOne<T>().WithMany().HasForeignKey()`
- Repositories: Scoped lifetime
- snake_case naming convention applied globally

**Query Handlers:**
- ONLY Dapper (ISqlConnectionFactory) — never EF Core
- `using var connection = _sqlConnectionFactory.CreateConnection();`
- `const string sql = """...""";` (raw string literals)
- snake_case SQL columns aliased to PascalCase

**Controllers:**
- Manual Result → HTTP mapping
- POST: `CreatedAtAction(nameof(GetMethod), new { id = result.Value }, result.Value)`
- GET single: `result.IsSuccess ? Ok(result.Value) : NotFound()`
- GET list: `Ok(result.Value)`
- PUT/DELETE: `result.IsSuccess ? NoContent() : BadRequest/NotFound`
- `[HasPermission("...")]` for authorized endpoints
</conventions>

### Step 5: Build Verification

After all files are created:

```bash
dotnet build Bookify.sln
```

Fix any compilation errors before presenting the result.

### Step 6: Migration Reminder

Remind the user to create an EF Core migration:

```bash
dotnet ef migrations add Add{EntityName} --project Bookify.Infrastructure --startup-project Bookify.WebApi
dotnet ef database update --project Bookify.Infrastructure --startup-project Bookify.WebApi
```

### Verification Checklist

- [ ] Domain layer has zero external dependencies (only MediatR.Contracts for IDomainEvent)
- [ ] Entity has private constructors + factory method
- [ ] Domain events raised in entity, handled in Application layer
- [ ] Commands go through EF Core (write), Queries through Dapper (read)
- [ ] Response DTOs are flat `sealed class` with `{ get; init; }`
- [ ] Validators use FluentValidation `AbstractValidator<TCommand>`
- [ ] Repository interface in Domain, implementation in Infrastructure
- [ ] EF Config uses correct VO conversions and FK patterns
- [ ] Controller uses ISender and manual Result mapping
- [ ] DI registration added for new repository
- [ ] All naming conventions followed
- [ ] No navigation properties between aggregates
- [ ] Build succeeds
