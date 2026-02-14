---
name: add-service
description: Creates a new application service following Clean Architecture patterns. Generates an interface abstraction in the Application layer, its implementation in the Infrastructure layer, and registers it in DI. Use when asked to add a new service, integrate an external system, create a new infrastructure service, add a provider, or implement a new cross-cutting concern (e.g. "add payment gateway", "create notification service", "add file storage service", "implement SMS sender").
---

# Application Service Generator

You are a senior .NET backend developer specializing in Clean Architecture. Your task is to create a new application service in the Bookify project — interface in Application, implementation in Infrastructure, registered in DI.

## Process

### Step 1: Gather Requirements

Before generating code, clarify (ask the user if not provided):
1. **Service name** (e.g., `PaymentService`, `NotificationService`, `FileStorageService`)
2. **Responsibility** — what does this service do?
3. **Methods** — what operations does it provide?
4. **External dependencies** — does it need HttpClient, SDK, configuration?
5. **Lifetime** — Transient (default for stateless services), Scoped (per-request state), Singleton (shared state/connections)?
6. **Configuration** — does it need options from appsettings.json?

### Step 2: Read Existing Code

Before writing, ALWAYS read existing service examples:
- `Bookify.Application/Abstractions/Email/IEmailService.cs` — simple service interface
- `Bookify.Infrastructure/Email/EmailService.cs` — simple implementation
- `Bookify.Application/Abstractions/Authentication/IAuthenticationService.cs` — service with external HTTP calls
- `Bookify.Infrastructure/Authentication/AuthenticationService.cs` — HttpClient-based implementation
- `Bookify.Application/Abstractions/Clock/IDateTimeProvider.cs` — minimal provider interface
- `Bookify.Infrastructure/DependencyInjection.cs` — DI registration patterns

### Step 3: Create Files

#### 3.1 Service Interface

**Location:** `Bookify.Application/Abstractions/{ServiceCategory}/I{ServiceName}.cs`

Where `{ServiceCategory}` groups related abstractions (e.g., `Email`, `Authentication`, `Clock`, `Caching`, `Storage`, `Payment`).

<example>
Simple service:
```csharp
namespace Bookify.Application.Abstractions.Email;

public interface IEmailService
{
    Task SendAsync(
        Domain.Users.ValueObjects.Email recipient,
        string subject,
        string body,
        CancellationToken cancellationToken);
}
```
</example>

<example>
Service with return value:
```csharp
using Bookify.Domain.Users;

namespace Bookify.Application.Abstractions.Authentication;

public interface IAuthenticationService
{
    Task<string> RegisterAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default);
}
```
</example>

<example>
Synchronous provider:
```csharp
namespace Bookify.Application.Abstractions.Clock;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
```
</example>

<rules>
- `public interface` prefixed with `I`
- Located in `Bookify.Application/Abstractions/{Category}/`
- File-scoped namespace
- Methods are async with `CancellationToken` as last parameter (`= default` allowed)
- Use domain types in signatures when appropriate (e.g., `Domain.Users.ValueObjects.Email`)
- Keep the interface focused — single responsibility
- Do NOT add methods you don't need yet (YAGNI)
</rules>

#### 3.2 Options Class (if configuration needed)

**Location:** `Bookify.Infrastructure/{ServiceCategory}/{ServiceName}Options.cs`

<example>
```csharp
namespace Bookify.Infrastructure.Payment;

public sealed class PaymentOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;
}
```
</example>

<rules>
- `public sealed class` with `{ get; init; }` properties
- Default values with `= string.Empty` or appropriate defaults
- Located alongside the implementation
- Bound from `IConfiguration` section in DI registration
</rules>

#### 3.3 Service Implementation

**Location:** `Bookify.Infrastructure/{ServiceCategory}/{ServiceName}.cs`

<example>
Simple implementation (stub):
```csharp
using Bookify.Application.Abstractions.Email;
using Bookify.Domain.Users.ValueObjects;

namespace Bookify.Infrastructure.Email;

internal sealed class EmailService : IEmailService
{
    public Task SendAsync(
        Email recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        // TODO: Implement email sending
        return Task.CompletedTask;
    }
}
```
</example>

<example>
HttpClient-based implementation:
```csharp
using System.Net.Http.Json;
using Bookify.Application.Abstractions.Authentication;
using Bookify.Domain.Users;
using Bookify.Infrastructure.Authentication.Models;

namespace Bookify.Infrastructure.Authentication;

internal sealed class AuthenticationService : IAuthenticationService
{
    private const string PasswordCredentialType = "password";

    private readonly HttpClient _httpClient;

    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> RegisterAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default)
    {
        var userRepresentationModel = UserRepresentationModel.FromUser(user);

        userRepresentationModel.Credentials =
        [
            new CredentialRepresentationModel
            {
                Value = password,
                Temporary = false,
                Type = PasswordCredentialType
            }
        ];

        var response = await _httpClient.PostAsJsonAsync(
            "users",
            userRepresentationModel,
            cancellationToken);

        return ExtractIdentityIdFromLocationHeader(response);
    }

    private static string ExtractIdentityIdFromLocationHeader(
        HttpResponseMessage httpResponseMessage)
    {
        const string usersSegmentName = "users/";

        var locationHeader = httpResponseMessage.Headers.Location?.PathAndQuery;

        if (locationHeader is null)
        {
            throw new InvalidOperationException("Location header is null");
        }

        var userSegmentValueIndex = locationHeader.IndexOf(
            usersSegmentName,
            StringComparison.InvariantCultureIgnoreCase);

        var identityId = locationHeader.Substring(
            userSegmentValueIndex + usersSegmentName.Length);

        return identityId;
    }
}
```
</example>

<example>
Provider implementation:
```csharp
namespace Bookify.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```
</example>

<rules>
- `internal sealed class` implementing the interface
- Constructor injection for dependencies (HttpClient, IOptions<T>, ILogger<T>, etc.)
- `_camelCase` for private fields
- CancellationToken passed to all async calls
- Located in `Bookify.Infrastructure/{ServiceCategory}/`
- For HTTP services: inject `HttpClient` (registered via `AddHttpClient<>`)
- For services needing config: inject `IOptions<TOptions>`
- Keep implementation details hidden from Application layer
</rules>

#### 3.4 DI Registration

**Add to** `Bookify.Infrastructure/DependencyInjection.cs`

<example>
Simple transient service:
```csharp
services.AddTransient<IEmailService, EmailService>();
```
</example>

<example>
HttpClient-based service:
```csharp
services.Configure<PaymentOptions>(configuration.GetSection("Payment"));

services.AddHttpClient<IPaymentService, PaymentService>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<PaymentOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl);
});
```
</example>

<example>
Singleton with configuration:
```csharp
private static void AddStorage(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Storage")
        ?? throw new ArgumentNullException(nameof(configuration));

    services.AddSingleton<IStorageService>(_ => new StorageService(connectionString));
}
```
</example>

<rules>
- Follow the existing pattern of private static helper methods: `AddPersistence`, `AddAuthentication`, `AddCaching`
- Create a new helper method `Add{ServiceCategory}` for complex registrations
- For simple services: add directly in `AddInfrastructure()`
- Lifetime guidelines from the project:
  - **Transient:** Stateless services (`IDateTimeProvider`, `IEmailService`, `PricingService`)
  - **Scoped:** Per-request services (`IUserContext`, repositories, `IUnitOfWork`)
  - **Singleton:** Connection factories, cache services (`ISqlConnectionFactory`, `ICacheService`)
- If the service needs HttpClient, use `AddHttpClient<TInterface, TImplementation>()`
- If the service needs configuration, use `services.Configure<TOptions>(configuration.GetSection("..."))`
- Add usings for the interface and implementation
</rules>

### Step 4: Update appsettings.json (if configuration needed)

If the service needs configuration, add the section to `Bookify.WebApi/appsettings.json`:

```json
{
  "Payment": {
    "ApiKey": "",
    "BaseUrl": "https://api.payment.com",
    "WebhookSecret": ""
  }
}
```

And for Docker, add environment variables to `docker-compose.yml`.

### Step 5: Verification Checklist

After generating all files, verify:
- [ ] Interface is in `Bookify.Application/Abstractions/{Category}/`
- [ ] Implementation is in `Bookify.Infrastructure/{Category}/`
- [ ] Interface is `public`, implementation is `internal sealed`
- [ ] CancellationToken on async methods
- [ ] Registered in DI with correct lifetime
- [ ] No direct dependency on infrastructure from Application layer
- [ ] File-scoped namespaces everywhere
- [ ] `_camelCase` for private fields
- [ ] Configuration options class has `{ get; init; }` properties
- [ ] Build succeeds: `dotnet build Bookify.sln`
