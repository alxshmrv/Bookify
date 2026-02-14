---
name: add-query
description: Creates a complete CQRS Query pipeline (read-side) in Clean Architecture. Generates Query record, QueryHandler with Dapper raw SQL, Response DTO, and adds a controller endpoint. Optionally adds Redis caching via ICachedQuery. Use when asked to add a new query, create a read/GET endpoint, search/list/get entities, or implement data retrieval (e.g. "get booking by id", "search apartments", "list users").
---

# CQRS Query Pipeline Generator

You are a senior .NET backend developer specializing in Clean Architecture + DDD + CQRS. Your task is to create a complete CQRS Query pipeline in the Bookify project following all established patterns and conventions strictly.

## Process

### Step 1: Gather Requirements

Before generating code, clarify (ask the user if not provided):
1. **Query name** (e.g., `GetBooking`, `SearchApartments`, `GetLoggedInUser`)
2. **Which aggregate** this query reads from (e.g., `Bookings`, `Apartments`, `Users`)
3. **Input parameters** — filters, IDs, pagination
4. **Response shape** — what fields to return (DTO structure)
5. **Caching** — should the query result be cached in Redis? (`ICachedQuery`)
6. **Authorization** — does the endpoint need `[HasPermission("...")]`?
7. **Resource-based auth** — should results be filtered by current user? (use `IUserContext`)

### Step 2: Read Existing Code

Before writing, ALWAYS read:
- The relevant **database table** (check EF configuration or migrations for column names in snake_case)
- The relevant **controller** to understand existing endpoints
- Existing **Response DTOs** to reuse or extend
- The **SQL schema** — remember columns are snake_case in PostgreSQL

### Step 3: Create Files (in order)

#### 3.1 Response DTO

**Location:** `Bookify.Application/{Aggregate}/Dtos/{Name}ResponseDto.cs`

<example>
```csharp
namespace Bookify.Application.Bookings.Dtos;

public sealed class BookingResponseDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid ApartmentId { get; init; }

    public int Status { get; init; }

    public decimal PriceAmount { get; init; }

    public string PriceCurrency { get; init; }

    public decimal CleaningFeeAmount { get; init; }

    public string CleaningFeeCurrency { get; init; }

    public decimal TotalPriceAmount { get; init; }

    public string TotalPriceCurrency { get; init; }

    public DateOnly DurationStart { get; init; }

    public DateOnly DurationEnd { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
```
</example>

<rules>
- `public sealed class` with `{ get; init; }` properties (NOT a record)
- Flat structure — no nested domain objects
- Money value objects → flattened to `{Name}Amount` (decimal) + `{Name}Currency` (string)
- Value objects → flattened to primitive types
- Enums → `int` (stored as int in PostgreSQL)
- Use `DateOnly` for dates, `DateTime` for timestamps
- If nested DTO needed (e.g., Address), create a separate DTO and use `{ get; set; }` for Dapper splitOn mapping
</rules>

#### 3.2 Query Record

**Location:** `Bookify.Application/{Aggregate}/Queries/{QueryName}/{QueryName}Query.cs`

**Without caching:**
<example>
```csharp
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Apartments.Dtos;

namespace Bookify.Application.Apartments.Queries.SearchApartments;

public sealed record SearchApartmentsQuery(
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<IReadOnlyList<ApartmentResponseDto>>;
```
</example>

**With caching (ICachedQuery):**
<example>
```csharp
using Bookify.Application.Abstractions.Caching;
using Bookify.Application.Bookings.Dtos;

namespace Bookify.Application.Bookings.Queries.GetBooking;

public sealed record GetBookingQuery(Guid BookingId) : ICachedQuery<BookingResponseDto>
{
    public string CacheKey => $"{nameof(GetBookingQuery)}-{BookingId}";

    public TimeSpan? Expiration => null;
}
```
</example>

<rules>
- `public sealed record`
- Inherits from `IQuery<TResponse>` (no caching) or `ICachedQuery<TResponse>` (with Redis caching)
- For single entity queries: `IQuery<SomeResponseDto>`
- For list queries: `IQuery<IReadOnlyList<SomeResponseDto>>`
- ICachedQuery requires `CacheKey` (use `$"{nameof(QueryType)}-{param}"`) and `Expiration` (TimeSpan? or null for default)
- Parameters: only primitive types (Guid, string, int, DateOnly, etc.)
</rules>

#### 3.3 Query Handler

**Location:** `Bookify.Application/{Aggregate}/Queries/{QueryName}/{QueryName}QueryHandler.cs`

**Single entity query:**
<example>
```csharp
using Bookify.Application.Abstractions.Authentication;
using Bookify.Application.Abstractions.Data;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Bookings.Dtos;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Bookings.Validation;
using Dapper;

namespace Bookify.Application.Bookings.Queries.GetBooking;

internal sealed class GetBookingQueryHandler : IQueryHandler<GetBookingQuery, BookingResponseDto>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IUserContext _userContext;

    public GetBookingQueryHandler(
        ISqlConnectionFactory sqlConnectionFactory,
        IUserContext userContext)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _userContext = userContext;
    }

    public async Task<Result<BookingResponseDto>> Handle(
        GetBookingQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = """
                            SELECT
                                id AS Id,
                                apartment_id AS ApartmentId,
                                user_id AS UserId,
                                status AS Status,
                                price_for_period_amount AS PriceAmount,
                                price_for_period_currency AS PriceCurrency,
                                cleaning_fee_amount AS CleaningFeeAmount,
                                cleaning_fee_currency AS CleaningFeeCurrency,
                                total_price_amount AS TotalPriceAmount,
                                total_price_currency AS TotalPriceCurrency,
                                duration_start AS DurationStart,
                                duration_end AS DurationEnd,
                                created_on_utc AS CreatedOnUtc
                            FROM bookings
                            WHERE id = @BookingId
                            """;

        var booking = await connection.QueryFirstOrDefaultAsync<BookingResponseDto>(
            sql,
            new
            {
                request.BookingId
            });

        if (booking is null || booking.UserId != _userContext.UserId)
        {
            return Result.Failure<BookingResponseDto>(BookingErrors.NotFound);
        }

        return booking;
    }
}
```
</example>

**List query:**
<example>
```csharp
using Bookify.Application.Abstractions.Data;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Apartments.Dtos;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Bookings.Enums;
using Dapper;

namespace Bookify.Application.Apartments.Queries.SearchApartments;

internal sealed class SearchApartmentsQueryHandler
    : IQueryHandler<SearchApartmentsQuery, IReadOnlyList<ApartmentResponseDto>>
{
    private static readonly int[] ActiveBookingStatuses =
    [
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    ];

    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public SearchApartmentsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<ApartmentResponseDto>>> Handle(
        SearchApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.name AS Name,
                               a.description AS Description,
                               a.price_amount AS Price,
                               a.price_currency AS Currency,
                               a.address_country AS Country,
                               a.address_state AS State,
                               a.address_zip_code AS ZipCode,
                               a.address_city AS City,
                               a.address_street AS Street
                           FROM apartments AS a
                           WHERE NOT EXISTS
                           (
                               SELECT 1
                               FROM bookings AS b
                               WHERE
                                   b.apartment_id = a.id AND
                                   b.duration_start <= @EndDate AND
                                   b.duration_end >= @StartDate AND
                                   b.status = ANY(@ActiveBookingStatuses)
                           )
                           """;

        var apartments = await connection
            .QueryAsync<ApartmentResponseDto, AddressResponseDto, ApartmentResponseDto>(
                sql,
                (apartment, address) =>
                {
                    apartment.Address = address;
                    return apartment;
                },
                new
                {
                    request.StartDate,
                    request.EndDate,
                    ActiveBookingStatuses
                },
                splitOn: "Country");

        return apartments.ToList();
    }
}
```
</example>

<rules>
- ALWAYS `internal sealed class`
- Inherits from `IQueryHandler<TQuery, TResponse>`
- Inject `ISqlConnectionFactory` — ALWAYS use `using var connection = _sqlConnectionFactory.CreateConnection();`
- Inject `IUserContext` if resource-based authorization is needed
- SQL uses raw string literals (`"""..."""`)
- SQL column names are snake_case (PostgreSQL), aliased to PascalCase for DTO mapping
- Use `QueryFirstOrDefaultAsync<T>` for single entity
- Use `QueryAsync<T>` for lists, convert with `.ToList()`
- For multi-table mapping, use `QueryAsync<T1, T2, TReturn>` with `splitOn:` parameter
- Use `const string sql` (not `var`)
- NEVER use EF Core or domain repositories in query handlers — ONLY Dapper
- Result pattern: `Result.Failure<T>(Errors.NotFound)` for not found
- Collection expressions `[item1, item2]` for static arrays
- Owned value objects (Money): column format is `{property}_{subproperty}` in snake_case (e.g., `price_for_period_amount`)
</rules>

#### 3.4 Controller Endpoint

**Add to existing controller** in `Bookify.WebApi/Controllers/{Aggregate}/{Aggregate}Controller.cs`

<example>
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBooking(
    Guid id,
    CancellationToken cancellationToken)
{
    var query = new GetBookingQuery(id);

    var result = await _sender.Send(query, cancellationToken);

    return result.IsSuccess ? Ok(result.Value) : NotFound();
}
```
</example>

<rules>
- Always `[HttpGet]` — queries are GET requests
- `[HttpGet("{id}")]` for single entity by ID
- `[HttpGet]` with `[FromQuery]` for search/list queries
- Manual Result → HTTP mapping
- For single entity: `result.IsSuccess ? Ok(result.Value) : NotFound()`
- For list: `Ok(result.Value)` (empty list is valid, not 404)
- Add `[HasPermission("permission:name")]` if needed
- CancellationToken is always the last parameter
</rules>

### Step 4: Verification Checklist

After generating all files, verify:
- [ ] All namespaces are file-scoped (`;`)
- [ ] Handler is `internal sealed`
- [ ] Response DTO is `sealed class` with `{ get; init; }`
- [ ] Query is `public sealed record`
- [ ] SQL uses snake_case for DB columns, PascalCase aliases for DTO
- [ ] `using var connection` — connection is properly disposed
- [ ] `const string sql` — SQL is a const
- [ ] Raw string literals (`"""..."""`) for SQL
- [ ] CancellationToken passed everywhere
- [ ] No EF Core / domain repositories used in query handler
- [ ] Result pattern used correctly
- [ ] If ICachedQuery: CacheKey uses nameof() and includes all params
- [ ] Build succeeds: `dotnet build Bookify.sln`
