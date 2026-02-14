Act as a Senior .NET C# Architect and Technical Lead. 

I need you to perform a deep recursive analysis of this entire repository to generate the ultimate, comprehensive `CLAUDE.md` file. Do not just use a template; reverse-engineer the project structure and rules from the existing code.

**Step 1: Deep Analysis**
Scan all `.cs`, `.csproj`, `.sln`, `appsettings.json`, and `Dockerfile` files to determine:

1.  **Architecture & High-Level Structure:**
    *   Identify the architectural style (e.g., Clean Architecture, Onion, Vertical Slice, N-Tier, Modular Monolith).
    *   Map out the Project/Solution structure (Domain, Application, Infrastructure, API, etc.).
    *   Identify entry points (`Program.cs`, `Startup.cs`).

2.  **Tech Stack & Dependencies:**
    *   List key frameworks (target frameworks like .NET 6/8/9, ASP.NET Core).
    *   Identify core libraries (EF Core, Dapper, MediatR, AutoMapper, FluentValidation, MassTransit, Serilog, etc.).

3.  **Design Patterns & Implementation Details:**
    *   Identify used GoF patterns (Repository, Unit of Work, Factory, Strategy, Singleton, etc.).
    *   Identify architectural patterns (CQRS, Event Sourcing, Mediator pipeline behaviors).
    *   Analyze how Dependency Injection is configured.

4.  **Conventions & Coding Style:**
    *   Analyze naming conventions (Interfaces start with 'I', Async methods end with 'Async', etc.).
    *   Check for specific coding rules (usage of `var` vs explicit types, file-scoped namespaces, global usings).
    *   Analyze Error Handling strategies (Middleware, Result pattern, Exceptions).
    *   Analyze Validation logic (Data Annotations vs FluentValidation).

5.  **Testing & QA:**
    *   Identify testing frameworks (xUnit, NUnit, MSTest).
    *   Identify mocking libraries (Moq, NSubstitute).
    *   Analyze test structure (Unit, Integration, E2E) and naming conventions for tests.
    *   Look for snapshot testing or architectural tests (NetArchTest).

**Step 2: Generate CLAUDE.md**
Rewrite the `CLAUDE.md` file to include the following sections with maximum detail based on your analysis:

# Project: [Project Name]

## 🛠 Build & Run
*   **Build:** [Command]
*   **Run:** [Command]
*   **Test:** [Command]
*   **Lint/Format:** [Command]
*   **Database Migrations:** [Commands for Add/Update]

## 🏗 Architecture & Structure
*   **Style:** [Architecture Name]
*   **Key Projects/Layers:**
    *   `[ProjectName]`: [Responsibility]
*   **Data Flow:** Describe how a request travels through the layers.

## 🧩 Key Patterns & Technologies
*   **Patterns:** List all detected patterns (e.g., CQRS via MediatR, Repository Pattern).
*   **Stack:** List versions and key libs (EF Core, Redis, etc.).
*   **Validation:** How is validation handled?
*   **Logging:** Strategy and libraries.

## 📝 Coding Conventions (Strict)
*   **Namespaces:** [e.g., File-scoped]
*   **Naming:** [e.g., PascalCase for public, _camelCase for private fields]
*   **Async:** [Rules regarding async/await]
*   **DTOs/Records:** [Preferences: record vs class]
*   **Nullability:** [Is <Nullable>enable</Nullable>?]

## 🧪 Testing Strategy
*   **Frameworks:** [xUnit/NUnit]
*   **Conventions:** [Naming of test methods, e.g., Given_When_Then]
*   **Mocks:** [Library used]
*   **Integration Tests:** Setup details (TestContainers, InMemory DB?).

## ⚠️ Critical Implementation Details
*   Mention any specific "gotchas" or strict rules found in the codebase (e.g., "Always use CancellationToken in controllers", "Never expose Domain Entities directly in API").

---

**Action:**
Perform the scan now and overwrite `CLAUDE.md` with this detailed content.