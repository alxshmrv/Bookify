---
name: add-command
description: Creates a complete CQRS Command pipeline (write-side) in Clean Architecture. Generates Command record, CommandHandler, FluentValidation validator, Request DTO, and adds a controller endpoint. Use when asked to add a new command, create a write operation, add a POST/PUT/DELETE endpoint, or implement a new business action (e.g. "create booking", "update user", "cancel order").
---

# CQRS Command Pipeline Generator

You are a senior .NET backend developer specializing in Clean Architecture + DDD + CQRS. Your task is to create a complete CQRS Command pipeline in the Bookify project following all established patterns and conventions strictly.

## Process

### Step 1: Gather Requirements

Before generating code, clarify (ask the user if not provided):
1. **Command name** (e.g., `ConfirmBooking`, `CreateApartment`, `UpdateUser`)
2. **Which aggregate** this command operates on (e.g., `Bookings`, `Apartments`, `Users`)
3. **Input parameters** — what data the command needs
4. **Return type** — `ICommand` (returns `Result`) or `ICommand<TResponse>` (returns `Result<TResponse>`, e.g., `ICommand<Guid>` for creation)
5. **Validation rules** — what FluentValidation rules are needed
6. **Authorization** — does the endpoint need `[HasPermission("...")]`?
7. **HTTP method** — POST (create), PUT (update), DELETE (delete), PATCH (partial update)

### Step 2: Read Existing Code

Before writing, ALWAYS read:
- The relevant **domain entity** to understand available methods and properties
- The relevant **repository interface** to know available methods
- The relevant **controller** to understand existing endpoints and routing
- The relevant **Errors class** to reuse existing error definitions

### Step 3: Create Files (in order)

#### 3.1 Command Record

**Location:** `Bookify.Application/{Aggregate}/Commands/{CommandName}/{CommandName}Command.cs`

<example>
```csharp
using Bookify.Application.Abstractions.Messaging;

namespace Bookify.Application.Bookings.Commands.ReserveBooking;

public record ReserveBookingCommand(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate) : ICommand<Guid>;
```
</example>

<rules>
- ALWAYS a `record`, not a class
- Inherits from `ICommand` (no return) or `ICommand<TResponse>` (with return value)
- Use `ICommand<Guid>` for creation commands that return the new entity's ID
- File-scoped namespace with `;`
- Only `using Bookify.Application.Abstractions.Messaging;` is required
- Parameters should be primitive types or simple value types (Guid, string, DateOnly, etc.) — NOT domain objects
</rules>

#### 3.2 Command Handler

**Location:** `Bookify.Application/{Aggregate}/Commands/{CommandName}/{CommandName}CommandHandler.cs`

<example>
```csharp
using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Exceptions;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Apartments;
using Bookify.Domain.Apartments.Validation;
using Bookify.Domain.Bookings;
using Bookify.Domain.Bookings.Services;
using Bookify.Domain.Bookings.Validation;
using Bookify.Domain.Bookings.ValueObjects;
using Bookify.Domain.Users;
using Bookify.Domain.Users.Validation;

namespace Bookify.Application.Bookings.Commands.ReserveBooking;

internal sealed class ReserveBookingCommandHandler : ICommandHandler<ReserveBookingCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PricingService _pricingService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReserveBookingCommandHandler(
        IUserRepository userRepository,
        IApartmentRepository apartmentRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        PricingService pricingService,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _apartmentRepository = apartmentRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(
        ReserveBookingCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound);
        }

        var apartment = await _apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<Guid>(ApartmentErrors.NotFound);
        }

        var duration = DateRange.Create(request.StartDate, request.EndDate);

        if (await _bookingRepository.IsOverlappingAsync(apartment, duration.Value, cancellationToken))
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }

        try
        {
            var booking = Booking.Reserve(
                apartment,
                user.Id,
                duration.Value,
                _dateTimeProvider.UtcNow,
                _pricingService);

            _bookingRepository.Add(booking.Value);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return booking.Value.Id;
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }
    }
}
```
</example>

<rules>
- ALWAYS `internal sealed class`
- Inherits from `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>`
- Constructor injection with `_camelCase` private readonly fields
- `Handle` method signature: `public async Task<Result<TResponse>> Handle(TCommand request, CancellationToken cancellationToken)`
- CancellationToken is always passed through to ALL async calls
- CancellationToken has NO `= default` in handler (unlike repositories)
- Use Result pattern: `Result.Failure<T>(SomeErrors.SomeError)` for failures
- Use `var` for obviously typed variables
- Check for null with `is null` / `is not null`
- Always call `_unitOfWork.SaveChangesAsync(cancellationToken)` after mutations
- Wrap in try/catch for `ConcurrencyException` only when optimistic concurrency is involved
- Use entities' factory methods (e.g., `Booking.Reserve(...)`) — NEVER call constructors directly
</rules>

#### 3.3 FluentValidation Validator

**Location:** `Bookify.Application/{Aggregate}/Validation/{CommandName}CommandValidator.cs`

<example>
```csharp
using Bookify.Application.Bookings.Commands.ReserveBooking;
using FluentValidation;

namespace Bookify.Application.Bookings.Validation;

public class ReserveBookingCommandValidator : AbstractValidator<ReserveBookingCommand>
{
    public ReserveBookingCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();

        RuleFor(c => c.ApartmentId).NotEmpty();

        RuleFor(c => c.StartDate).LessThan(c => c.EndDate);
    }
}
```
</example>

<rules>
- `public class` (NOT sealed — FluentValidation requirement)
- Inherits from `AbstractValidator<TCommand>`
- Rules defined in parameterless constructor
- Validation is for input correctness ONLY (not business logic)
- Common rules: `NotEmpty()` for Guids, `NotEmpty()`/`NotNull()` for strings, `GreaterThan(0)` for amounts, `LessThan()` for date ranges
- Business validation belongs in the handler/domain, NOT here
</rules>

#### 3.4 Request DTO

**Location:** `Bookify.Application/{Aggregate}/Dtos/{CommandName}RequestDto.cs`

<example>
```csharp
namespace Bookify.Application.Bookings.Dtos;

public sealed record ReserveBookingRequestDto(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate);
```
</example>

<rules>
- `public sealed record` with positional parameters
- Contains the same fields as the Command (or a subset if some come from route/auth context)
- File-scoped namespace
- No validation attributes — validation is in FluentValidation
</rules>

#### 3.5 Controller Endpoint

**Add to existing controller** in `Bookify.WebApi/Controllers/{Aggregate}/{Aggregate}Controller.cs`

<example>
```csharp
[HttpPost]
public async Task<IActionResult> ReserveBooking(
    ReserveBookingRequestDto request,
    CancellationToken cancellationToken)
{
    var command = new ReserveBookingCommand(
        request.ApartmentId,
        request.UserId,
        request.StartDate,
        request.EndDate);

    var result = await _sender.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        return BadRequest(result.Error);
    }

    return CreatedAtAction(nameof(GetBooking), new { id = result.Value }, result.Value);
}
```
</example>

<rules>
- Add to EXISTING controller — do NOT create a new controller unless the aggregate has none
- Manual Result → HTTP mapping: `result.IsSuccess ? Ok(...) : BadRequest/NotFound(...)`
- For creation (POST): return `CreatedAtAction(nameof(GetMethod), new { id = result.Value }, result.Value)`
- For update (PUT/PATCH): return `Ok()` or `NoContent()`
- For delete (DELETE): return `NoContent()`
- For failure: return `BadRequest(result.Error)` or `NotFound()` depending on context
- `[HttpPost]`, `[HttpPut("{id}")]`, `[HttpDelete("{id}")]` as appropriate
- Add `[HasPermission("permission:name")]` if authorization is needed
- Always accept `CancellationToken cancellationToken` as last parameter
- Map DTO → Command explicitly in the method body
</rules>

### Step 4: Add Errors (if needed)

If the command introduces new error cases, add them to `Bookify.Domain/{Aggregate}/Validation/{Aggregate}Errors.cs`:

```csharp
public static readonly Error SomeError = new(
    "Aggregate.SomeError",
    "Description of what went wrong.");
```

### Step 5: Verification Checklist

After generating all files, verify:
- [ ] All namespaces are file-scoped (`;`)
- [ ] Handler is `internal sealed`
- [ ] DTO is `public sealed record`
- [ ] `CancellationToken` is last parameter everywhere, passed to all async calls
- [ ] No `= default` on `CancellationToken` in handler
- [ ] Result pattern used (no throwing exceptions for business logic)
- [ ] `var` used for obvious types
- [ ] `_camelCase` for private fields
- [ ] No domain entities returned to API (only DTOs)
- [ ] `IUnitOfWork.SaveChangesAsync()` called after mutations
- [ ] Entity factory methods used (not constructors)
- [ ] Build succeeds: `dotnet build Bookify.sln`
