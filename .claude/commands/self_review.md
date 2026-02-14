---
description: Self-review code changes between branches based on project conventions
argument-hint: [source-branch] [target-branch]
---

# Self Code Review

## Context

**Branches being compared:**
- Source branch: `$1`
- Target branch: `$2`

**Current git state:**
!`git branch --show-current`

**Changed files:**
!`git diff --name-only $2..$1`

**Diff statistics:**
!`git diff --stat $2..$1`

**Full diff for review:**
!`git diff $2..$1`

## Project Rules Reference

Review all changes against the project conventions defined in:
@.claude/CLAUDE.md

---

## Review Instructions

Perform a comprehensive self-review of the changes between branches `$2` and `$1`. Analyze each changed file against the project conventions and generate a detailed code review report.

### Scope Exclusions

**IMPORTANT: The following directories are OUT OF SCOPE for this review:**
- `src/datahub/` - External service, not our responsibility
- Any other external/third-party service directories

**Acceptable patterns (do NOT flag as issues):**
- Hardcoded strings in exception messages (e.g., `throw new InvalidOperationException("Слой не найден")`) - this is acceptable for domain-specific error messages

### 1. Architecture & Structure Compliance

Check for Clean Architecture layer violations:
- **Domain** (`*.Domain`) - Only entities and domain logic, no external dependencies
- **UseCases** (`*.UseCases`) - Commands/Queries via MediatR, no infrastructure code
- **Infrastructure.Interfaces** - Repository and service abstractions only
- **Infrastructure.Implementation** - EF Core, external service implementations
- **WebApi** - Controllers, middleware, DI composition root

Verify:
- [ ] No circular dependencies between layers
- [ ] Controllers only delegate to MediatR (`ISender`)
- [ ] No business logic in controllers
- [ ] Domain entities use `BaseEntity` or `BaseAuditableEntity`
- [ ] Domain events raised via `RaiseDomainEvent()`

### 2. CQRS Pattern & Naming Conventions

**Commands:**
- [ ] Naming: `{Entity}{Action}Command` (e.g., `UpdateLayerCommand`)
- [ ] Handler: `{Entity}{Action}CommandHandler`
- [ ] Location: `Handlers/{Entity}/Commands/{Action}/`
- [ ] Commands implement appropriate interface (`IRequest<T>`, `IProjectContextRequest<T>`, `ILayerContextRequest<T>`)

**Queries:**
- [ ] Naming: `{Entity}{Action}Query` (e.g., `GetProjectQuery`)
- [ ] Handler: `{Entity}{Action}QueryHandler`
- [ ] Location: `Handlers/{Entity}/Queries/{Action}/`

**DTOs:**
- [ ] Pattern: `{Entity}{Verb}[Request|Response]Dto`
- [ ] Use `record` types with `required` and `init`:
  ```csharp
  public sealed record ProjectResponseDto
  {
      public required Guid Id { get; init; }
      public required string Name { get; init; }
  }
  ```

### 3. Access Modifiers & Type Requirements

**Mandatory rules (enforced by Architecture Tests):**
- [ ] **Requests**: Must be `public readonly record struct`
  ```csharp
  public readonly record struct UpdateLayerCommand(Guid LayerId, string Name)
      : ILayerContextRequest<LayerResponseDto>;
  ```
- [ ] **Handlers**: Must be `internal sealed class`
  ```csharp
  internal sealed class UpdateLayerCommandHandler
      : IRequestHandler<UpdateLayerCommand, LayerResponseDto>
  ```
- [ ] **AutoMapper Profiles**: Must be `internal sealed class`
- [ ] **Pipeline Behaviors**: Must be `internal sealed class`
- [ ] **Notification Handlers**: Must be `sealed`

### 4. Validation (FluentValidation)

- [ ] Validators in `Handlers/{Entity}/Validation/` or `Handlers/{Entity}/Validators/`
- [ ] Validators implement `IPipelineBehaviorValidator` marker interface
- [ ] Validation rules use proper FluentValidation syntax:
  ```csharp
  RuleFor(e => e.WorkspaceId)
      .NotNull()
      .NotEmpty()
      .WithMessage("WorkspaceId must be not empty");
  ```
- [ ] All user inputs validated before processing

### 5. Authorization

- [ ] Appropriate authorization attributes used:
  - `[MinProjectRole(ProjectRoleLevel.X)]` for project-level checks
  - `[MinWorkspaceRole(WorkspaceRoleLevel.X)]` for workspace-level checks
- [ ] Role levels correctly chosen:
  - `Observer` (1) - Read-only
  - `Commentator` (2) - Can comment
  - `Approver` (3) - Can approve
  - `AttributeEditor` (4) - Edit attributes
  - `SpatialDataEditor` (5) - Edit geometry
  - `FullEditor` (6) - Full editing
  - `Administrator` (7) - Full control

### 6. Error Handling

- [ ] Proper exception types used:
  - `ValidationException` for validation failures
  - `DomainValidationException` for domain rule violations
  - `EntityNotFoundException` / `ProjectNotFoundException` for missing entities
  - `InvalidRequestException` for bad requests
- [ ] No generic exceptions thrown without proper handling

### 7. Unit of Work & Transactions

- [ ] Requests implement appropriate context interface (`IProjectContextRequest`, `ILayerContextRequest`) for automatic transaction handling
- [ ] No manual transaction management unless absolutely necessary
- [ ] Audit fields handled by UoW (don't set manually)

### 8. Outbox Pattern & Domain Events

- [ ] Domain events raised via `RaiseDomainEvent()` on entities
- [ ] Events converted to `OutboxMessage` automatically
- [ ] No direct publishing to RabbitMQ from handlers

### 9. Sagas (MassTransit State Machines)

If saga changes detected:
- [ ] Saga data implements `SagaStateMachineInstance`
- [ ] State machine inherits `MassTransitStateMachine<TSagaData>`
- [ ] Proper event correlation configured
- [ ] Fault handling implemented for each state

### 10. Testing

- [ ] Test naming convention: `Should_{ExpectedBehavior}_When_{Condition}`
  ```csharp
  public async Task Should_Have_Error_When_WorkspaceId_Is_Empty()
  ```
- [ ] FluentAssertions used for assertions
- [ ] FluentValidation.TestHelper for validator tests
- [ ] Architecture tests pass if structural changes made

### 11. Frontend (Vue 3/TypeScript)

If frontend changes detected:
- [ ] Composition API used (not Options API)
- [ ] TypeScript types properly defined
- [ ] Pinia for state management
- [ ] MapLibre GL for map operations
- [ ] EPSG:3857/EPSG:900913 coordinate system only

### 12. General Code Quality

- [ ] File-scoped namespaces preferred
- [ ] No package versions in `.csproj` (use `Directory.Packages.props`)
- [ ] No hardcoded secrets or credentials
- [ ] No commented-out code
- [ ] JSON uses `camelCase` property naming, enums as strings
- [ ] Proper `CancellationToken` propagation in async methods
- [ ] No over-engineering or unnecessary abstractions

### 13. Security Checks

- [ ] No SQL injection vulnerabilities (use parameterized queries/EF Core)
- [ ] No XSS vulnerabilities (proper output encoding)
- [ ] No command injection (avoid dynamic shell commands)
- [ ] Input validation at system boundaries
- [ ] Authorization checks on all sensitive endpoints

### 14. Корректность работы
- [ ] Race conditions и проблемы параллелизма
- [ ] Deadlock'и в async/await коде
- [ ] Неправильная обработка null/exceptions
- [ ] Нарушение бизнес-логики и инвариантов


### 15. Производительность
- [ ] N+1 запросы к БД
- [ ] Отсутствие индексов для частых запросов
- [ ] Неэффективное использование памяти
- [ ] Синхронные операции вместо асинхронных

### 16. Качество кода
- [ ] Нарушение принципов SOLID
- [ ] Дублирование логики
- [ ] Отсутствие валидации входных данных
- [ ] Магические числа и строки без констант
- [ ] **Комментарии в коде реализации (ЗАПРЕЩЕНО)**
---

## Skill Recommendations

After analyzing the changes, recommend relevant skills that could help improve or validate the implementation:

- **`/controller_endpoint_creator`** - If new API endpoints were added
- **`/cqrs_command_generator`** - If new commands were created that need review
- **`/cqrs_query_generator`** - If new queries were created that need review
- **`/domain_event_creator`** - If domain events or outbox patterns were implemented
- **`/entity_migration_helper`** - If new entities or migrations were added
- **`/fluentvalidation_builder`** - If validators were created or modified
- **`/masstransit_saga_designer`** - If sagas were added or modified
- **`/vue_composable_generator`** - If Vue components or composables were added

---

## Output Generation

Generate a comprehensive code review report as a Markdown file.

**Output location:** Create the file in the current working directory with naming pattern:
`code-review-{source-branch}-{timestamp}.md`

**Report structure:**
```markdown
# Code Review: $1 → $2
**Generated:** {current timestamp}
**Reviewer:** Claude Code (Self-Review)

## Summary
{Brief overview of changes: number of files, main areas affected}

## Files Changed
{List of all changed files with brief description of changes}

## Review Findings

### Critical Issues
{Issues that MUST be fixed before merge - violations of mandatory rules}

### Warnings
{Issues that SHOULD be addressed - potential problems or anti-patterns}

### Suggestions
{Optional improvements - code quality enhancements}

### Positive Findings
{Good practices observed in the changes}

## Compliance Checklist

### Architecture Compliance
- [ ] or [x] for each item...

### Naming Conventions
- [ ] or [x] for each item...

### Access Modifiers
- [ ] or [x] for each item...

### Validation & Authorization
- [ ] or [x] for each item...

### Testing
- [ ] or [x] for each item...

### Security
- [ ] or [x] for each item...

## Skill Recommendations
{List skills that could be invoked to help with the implementation}

## Conclusion
{Overall assessment: Ready for merge / Needs changes / Major rework required}
{List of action items if any}
```

Execute the review now and generate the output file.
