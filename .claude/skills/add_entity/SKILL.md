---
name: add-entity
description: Creates a complete Domain Aggregate with full infrastructure in Clean Architecture + DDD. Generates Entity class with factory methods, Value Objects, Repository interface and implementation, EF Core Configuration, Error definitions, and DI registration. Use when asked to create a new entity, aggregate, domain model, add a new table, or introduce a new business concept (e.g. "add Review entity", "create Payment aggregate", "new Notification model").
---

# Domain Aggregate Generator

You are a senior .NET backend developer specializing in Clean Architecture + DDD. Your task is to create a complete Domain Aggregate with all supporting infrastructure in the Bookify project following all established patterns and conventions strictly.

## Process

### Step 1: Gather Requirements

Before generating code, clarify (ask the user if not provided):
1. **Entity name** (e.g., `Review`, `Payment`, `Notification`)
2. **Properties** — what data the entity holds, which are value objects
3. **Factory method** — what static creation method (e.g., `Create`, `Submit`, `Place`)
4. **Domain events** — what events should be raised on creation/state changes
5. **Relationships** — FK to other aggregates (Apartment, User, Booking)
6. **Invariants** — business rules to enforce
7. **Value Objects** — which properties should be modeled as VOs
8. **Status/State machine** — does the entity have state transitions?

### Step 2: Read Existing Code

Before writing, ALWAYS read:
- `Bookify.Domain/Abstractions/Entity.cs` — base class
- `Bookify.Domain/Abstractions/Result.cs` — Result pattern
- `Bookify.Domain/Abstractions/Error.cs` — Error record
- An existing entity (e.g., `Booking.cs`) for reference patterns
- `Bookify.Infrastructure/DependencyInjection.cs` for DI registration pattern

### Step 3: Create Files (in order)

#### 3.1 Value Objects (if needed)

**Location:** `Bookify.Domain/{Aggregate}/ValueObjects/{VOName}.cs`

<example>
```csharp
namespace Bookify.Domain.Reviews.ValueObjects;

public sealed record Rating
{
    public static readonly Error Invalid = new("Rating.Invalid", "The rating is invalid.");

    private Rating(int value) => Value = value;

    public int Value { get; init; }

    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
        {
            return Result.Failure<Rating>(Invalid);
        }

        return new Rating(value);
    }
}
```
</example>

<rules>
- `public sealed record` for value objects
- Private constructor, static `Create()` factory method for validation
- Simple VOs (wrapping a single value): `public sealed record FirstName(string Value);`
- Complex VOs (with validation): private constructor + `Create()` returning `Result<T>`
- Use existing `Money` and `Currency` from `Bookify.Domain.Shared` — do NOT recreate
- Use existing `DateRange` from `Bookify.Domain.Bookings.ValueObjects` if applicable
</rules>

#### 3.2 Enums (if needed)

**Location:** `Bookify.Domain/{Aggregate}/Enums/{EnumName}.cs`

<example>
```csharp
namespace Bookify.Domain.Reviews.Enums;

public enum ReviewStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
```
</example>

#### 3.3 Domain Events

**Location:** `Bookify.Domain/{Aggregate}/Events/{EventName}DomainEvent.cs`

<example>
```csharp
using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Reviews.Events;

public record ReviewCreatedDomainEvent(Guid ReviewId) : IDomainEvent;
```
</example>

<rules>
- `public record` (NOT sealed for records implementing interfaces)
- Inherits from `IDomainEvent`
- Contains only the aggregate root ID (Guid)
- Name format: `{Aggregate}{Action}DomainEvent`
</rules>

#### 3.4 Errors Class

**Location:** `Bookify.Domain/{Aggregate}/Validation/{Aggregate}Errors.cs`

<example>
```csharp
using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Reviews.Validation;

public static class ReviewErrors
{
    public static readonly Error NotFound = new(
        "Review.NotFound",
        "The review was not found.");

    public static readonly Error NotEligible = new(
        "Review.NotEligible",
        "The review is not eligible because the booking is not yet completed.");
}
```
</example>

<rules>
- `public static class`
- Fields are `public static readonly Error`
- Error code format: `"{Aggregate}.{ErrorName}"`
- Descriptive error messages in English
</rules>

#### 3.5 Entity Class (Aggregate Root)

**Location:** `Bookify.Domain/{Aggregate}/{EntityName}.cs`

<example>
```csharp
using Bookify.Domain.Abstractions;
using Bookify.Domain.Reviews.Enums;
using Bookify.Domain.Reviews.Events;
using Bookify.Domain.Reviews.Validation;
using Bookify.Domain.Reviews.ValueObjects;

namespace Bookify.Domain.Reviews;

public sealed class Review : Entity
{
    private Review(
        Guid id,
        Guid apartmentId,
        Guid bookingId,
        Guid userId,
        Rating rating,
        Comment comment,
        DateTime createdOnUtc)
        : base(id)
    {
        ApartmentId = apartmentId;
        BookingId = bookingId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
        CreatedOnUtc = createdOnUtc;
    }

    private Review()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid BookingId { get; private set; }

    public Guid UserId { get; private set; }

    public Rating Rating { get; private set; }

    public Comment Comment { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public static Result<Review> Create(
        Guid apartmentId,
        Guid bookingId,
        Guid userId,
        Rating rating,
        Comment comment,
        DateTime utcNow)
    {
        var review = new Review(
            Guid.NewGuid(),
            apartmentId,
            bookingId,
            userId,
            rating,
            comment,
            utcNow);

        review.RaiseDomainEvent(new ReviewCreatedDomainEvent(review.Id));

        return review;
    }
}
```
</example>

<rules>
- `public sealed class` inheriting from `Entity`
- TWO constructors: private parameterized + private parameterless (EF Core)
- Properties: `public ... { get; private set; }` — encapsulated
- Static factory method returning `Result<T>` (e.g., `Create`, `Reserve`, `Submit`)
- Factory generates `Guid.NewGuid()` for ID
- `RaiseDomainEvent()` in factory method
- FKs to other aggregates: `public Guid ApartmentId { get; private set; }` (Guid, NOT navigation property)
- State transitions: separate methods returning `Result` (e.g., `Confirm()`, `Reject()`, `Cancel()`)
- State transition validates current status before changing
- DateTime properties: use `DateTime` type, named `{Action}OnUtc`
- Accept `DateTime utcNow` parameter (from `IDateTimeProvider`) — NEVER call `DateTime.UtcNow` directly
</rules>

#### 3.6 Repository Interface

**Location:** `Bookify.Domain/{Aggregate}/I{Entity}Repository.cs`

<example>
```csharp
namespace Bookify.Domain.Reviews;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Review review);
}
```
</example>

<rules>
- `public interface` in the Domain layer
- `GetByIdAsync` returns `Task<T?>` (nullable)
- `Add` is `void` (EF Core tracks entities)
- `CancellationToken` has `= default`
- Add domain-specific queries if needed (e.g., `IsOverlappingAsync`)
</rules>

#### 3.7 Repository Implementation

**Location:** `Bookify.Infrastructure/Repositories/{Entity}Repository.cs`

<example>
```csharp
using Bookify.Domain.Reviews;

namespace Bookify.Infrastructure.Repositories;

internal sealed class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }
}
```
</example>

<rules>
- `internal sealed class`
- Inherits from `Repository<T>` (base provides `GetByIdAsync` and `Add`)
- Override or add methods only when needed (e.g., custom queries)
- Constructor takes `ApplicationDbContext`
- `CancellationToken = default` on custom async methods
</rules>

#### 3.8 EF Core Configuration

**Location:** `Bookify.Infrastructure/Configurations/{Entity}Configuration.cs`

<example>
```csharp
using Bookify.Domain.Apartments;
using Bookify.Domain.Bookings;
using Bookify.Domain.Reviews;
using Bookify.Domain.Shared;
using Bookify.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookify.Infrastructure.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Rating)
            .HasConversion(rating => rating.Value, value => Rating.Create(value).Value);

        builder.Property(review => review.Comment)
            .HasConversion(comment => comment.Value, value => new Comment(value))
            .HasMaxLength(200);

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(review => review.ApartmentId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(review => review.BookingId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.UserId);
    }
}
```
</example>

<rules>
- `internal sealed class` implementing `IEntityTypeConfiguration<T>`
- `builder.ToTable("{tableName}")` — lowercase plural snake_case (e.g., "reviews", "bookings")
- `builder.HasKey(x => x.Id)` — always explicit
- **Money VOs:** `builder.OwnsOne(x => x.Price, priceBuilder => { priceBuilder.Property(m => m.Currency).HasConversion(...); })`
- **Simple VOs (single value):** `builder.Property(x => x.Name).HasConversion(vo => vo.Value, v => new VO(v))`
- **Complex VOs (with factory):** `builder.Property(x => x.Rating).HasConversion(r => r.Value, v => Rating.Create(v).Value)`
- **FKs between aggregates:** `builder.HasOne<OtherAggregate>().WithMany().HasForeignKey(x => x.OtherAggregateId)` — NO navigation properties
- **Owned entities (multi-value VOs):** `builder.OwnsOne(x => x.Duration)` for DateRange, Address, etc.
- **Optimistic concurrency** (if needed): `builder.Property<uint>("Version").IsRowVersion()`
- Snake_case naming convention is applied globally via `UseSnakeCaseNamingConvention()` — no need for column name mapping
</rules>

#### 3.9 DI Registration

**Add to** `Bookify.Infrastructure/DependencyInjection.cs` in the `AddPersistence` method:

```csharp
services.AddScoped<IReviewRepository, ReviewRepository>();
```

<rules>
- Repositories are ALWAYS `Scoped`
- Add the `using` for the domain interface and infrastructure implementation
- Add in the `AddPersistence` method alongside other repositories
</rules>

### Step 4: Verification Checklist

After generating all files, verify:
- [ ] Entity has private constructors (parameterized + parameterless)
- [ ] Entity has static factory method returning `Result<T>`
- [ ] Entity is `public sealed class : Entity`
- [ ] Properties are `{ get; private set; }`
- [ ] Domain events raised in factory/state-transition methods
- [ ] FKs are `Guid` properties — no navigation properties between aggregates
- [ ] Repository interface in Domain, implementation in Infrastructure
- [ ] Repository impl is `internal sealed`
- [ ] EF Config uses correct conversions for VOs
- [ ] EF Config FKs use `.HasOne<T>().WithMany().HasForeignKey()`
- [ ] Table name is lowercase plural (snake_case)
- [ ] Registered in DI as Scoped
- [ ] All namespaces are file-scoped
- [ ] Build succeeds: `dotnet build Bookify.sln`
