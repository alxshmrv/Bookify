---
name: add-domain-event
description: Creates a Domain Event and its handler following DDD patterns. Generates the event record in the Domain layer, the notification handler in the Application layer, and integrates RaiseDomainEvent into the entity. Use when asked to add a domain event, event handler, react to entity state changes, send notifications on actions, or implement side effects (e.g. "send email when booking confirmed", "notify on review created", "log when user registered").
---

# Domain Event & Handler Generator

You are a senior .NET backend developer specializing in Clean Architecture + DDD. Your task is to create a Domain Event with its handler in the Bookify project following all established patterns.

## Process

### Step 1: Gather Requirements

Before generating code, clarify (ask the user if not provided):
1. **Event name** (e.g., `BookingConfirmed`, `ReviewCreated`, `UserRegistered`)
2. **Which aggregate** raises this event
3. **When** is the event raised (on creation, state transition, specific action)
4. **Side effects** — what should happen when the event is handled (send email, update cache, sync data)
5. **Data needed** — what handler needs from the event (usually just the aggregate ID)

### Step 2: Read Existing Code

Before writing, ALWAYS read:
- The **entity** that will raise the event — to understand where `RaiseDomainEvent()` should be called
- Existing domain events in the entity's `Events/` folder for naming conventions
- Existing event handlers for pattern reference (e.g., `BookingReservedDomainEventHandler.cs`)

### Step 3: Create Files

#### 3.1 Domain Event Record

**Location:** `Bookify.Domain/{Aggregate}/Events/{EventName}DomainEvent.cs`

<example>
```csharp
using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Bookings.Events;

public record BookingReservedDomainEvent(Guid BookingId) : IDomainEvent;
```
</example>

<rules>
- `public record` inheriting from `IDomainEvent`
- Contains ONLY the aggregate root ID (e.g., `Guid BookingId`)
- Do NOT include other data — handler should load what it needs from repositories
- Name format: `{Aggregate}{Action}DomainEvent` (e.g., `BookingConfirmedDomainEvent`, `ReviewCreatedDomainEvent`)
- One file per event
- File-scoped namespace
</rules>

#### 3.2 Domain Event Handler

**Location:** `Bookify.Application/{Aggregate}/Commands/{RelatedCommand}/{EventName}DomainEventHandler.cs`

Or if not tied to a specific command: `Bookify.Application/{Aggregate}/Events/{EventName}DomainEventHandler.cs`

<example>
```csharp
using Bookify.Application.Abstractions.Email;
using Bookify.Domain.Bookings;
using Bookify.Domain.Bookings.Events;
using Bookify.Domain.Users;
using MediatR;

namespace Bookify.Application.Bookings.Commands.ReserveBooking;

internal sealed class BookingReservedDomainEventHandler : INotificationHandler<BookingReservedDomainEvent>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public BookingReservedDomainEventHandler(
        IBookingRepository bookingRepository,
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task Handle(
        BookingReservedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null)
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(booking.UserId, cancellationToken);

        if (user is null)
        {
            return;
        }

        await _emailService.SendAsync(
            user.Email,
            "Booking Reserved",
            $"Booking reserved for user {user.Email}",
            cancellationToken);
    }
}
```
</example>

<rules>
- `internal sealed class` implementing `INotificationHandler<TDomainEvent>`
- Uses `MediatR.INotificationHandler` (NOT IRequestHandler)
- Method signature: `public async Task Handle(TEvent notification, CancellationToken cancellationToken)`
- Load data from repositories using the event's aggregate ID — do NOT assume data is available
- NULL checks: if entity not found, `return` silently (event might be stale)
- CancellationToken passed to ALL async calls
- Can inject any service: repositories, `IEmailService`, `ICacheService`, etc.
- Handler processes side effects: emails, cache invalidation, cross-aggregate updates
- Domain events execute AFTER `SaveChangesAsync` — entities are already persisted
- NO `IUnitOfWork.SaveChangesAsync()` in event handler unless modifying other aggregates
- If modifying another aggregate in the handler, inject its `IUnitOfWork` and call `SaveChangesAsync`
</rules>

#### 3.3 Integrate with Entity

Add `RaiseDomainEvent()` call in the entity's factory or state-transition method:

<example>
```csharp
// In the entity's factory method or state transition
public Result Confirm(DateTime utcNow)
{
    if (Status != BookingStatus.Reserved)
    {
        return Result.Failure(BookingErrors.NotReserved);
    }

    Status = BookingStatus.Confirmed;
    ConfirmedOnUtc = utcNow;

    RaiseDomainEvent(new BookingConfirmedDomainEvent(Id));

    return Result.Success();
}
```
</example>

<rules>
- Call `RaiseDomainEvent(new EventName(Id))` AFTER state change
- Pass only `Id` (Guid) to the event constructor
- Events are collected in `Entity._domainEvents` list
- They are published by `ApplicationDbContext` AFTER `SaveChangesAsync`
- Multiple events can be raised in one method
</rules>

### Step 4: Verification Checklist

After generating all files, verify:
- [ ] Event is `public record : IDomainEvent`
- [ ] Event contains only aggregate root ID
- [ ] Handler is `internal sealed : INotificationHandler<TEvent>`
- [ ] Handler uses `notification` parameter (not `request`)
- [ ] Handler null-checks loaded entities and returns silently
- [ ] `RaiseDomainEvent()` called in entity after state change
- [ ] CancellationToken passed to all async calls
- [ ] File-scoped namespaces everywhere
- [ ] Build succeeds: `dotnet build Bookify.sln`
