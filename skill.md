# CliniKey — Engineering Standards & Skill File

> **This is the absolute source of truth.** Read this file before generating ANY code.
> If generated code contradicts any rule here, the code is wrong and this file is right.

---

## 1. Solution Identity

| Key | Value |
|-----|-------|
| Product | CliniKey — Dental Management SaaS (subscription model) |
| Market | Egypt |
| Language | **English-first**, with Arabic (`ar-EG`) localization |
| Runtime | .NET 10 / C# 13 |
| Solution Format | `.slnx` |
| Architecture | Clean Architecture + Vertical Slice Features |
| Primary DB | PostgreSQL (via EF Core + Dapper for reads) |
| Auth | ASP.NET Identity + JWT Bearer |
| CQRS Bus | MediatR 14.x |
| Multi-Tenancy | Schema-per-tenant (shared DB server) |
| Development Methodology | **Spec-Driven Development (SDD)** via SpecKit |

---

## 2. Spec-Driven Development — The Mandatory Workflow

### 2.1 The Golden Rule

> **Never write raw C# files manually if SpecKit can generate them from a specification.**
>
> Whenever you are asked to build a new entity, feature, module, or endpoint, you MUST
> follow the SDD pipeline below. Skipping steps is a violation of project standards.

### 2.2 The SDD Pipeline

Every new feature follows this exact sequence:

```
Step 1: specify → spec.md        (WHAT and WHY — no tech stack)
Step 2: clarify → spec.md        (refine ambiguity)
Step 3: plan    → plan.md        (HOW — tech stack, data model, contracts)
Step 4: tasks   → tasks.md       (actionable implementation checklist)
Step 5: implement                 (code generation from the spec artifacts)
```

### 2.3 SpecKit CLI Commands

The project has been initialized with SpecKit (Gemini integration). The CLI is at:
`C:\Users\PC\.local\bin\specify.exe`

All commands require `$env:PYTHONIOENCODING='utf-8'` prefix on Windows PowerShell.

| Step | Command | Output |
|------|---------|--------|
| Constitution | `/speckit.constitution` | `.specify/memory/constitution.md` |
| Specify | `/speckit.specify <description>` | `.specify/specs/{NNN}-{slug}/spec.md` |
| Clarify | `/speckit.clarify` | Updates spec with clarifications |
| Plan | `/speckit.plan <tech choices>` | `plan.md`, `data-model.md`, API contracts |
| Tasks | `/speckit.tasks` | `tasks.md` — ordered implementation steps |
| Analyze | `/speckit.analyze` | Cross-artifact consistency report |
| Checklist | `/speckit.checklist` | Quality validation checklist |
| Implement | `/speckit.implement` | Executes tasks, generates code |

### 2.4 SpecKit Directory Structure

```
.specify/
├── memory/
│   └── constitution.md          ← project principles (created once)
├── scripts/                     ← SpecKit helper scripts (create-new-feature, etc.)
├── templates/                   ← spec/plan/task templates
├── workflows/                   ← SDD workflow definitions
└── extensions/                  ← installed extensions

specs/                           ← lives at repo root, NOT inside .specify/
└── {NNN}-{feature-slug}/
    ├── spec.md                  ← functional specification
    ├── plan.md                  ← technical implementation plan
    ├── tasks.md                 ← task breakdown
    ├── data-model.md            ← entity/relationship model
    └── contracts/               ← API specs, DTOs
```

### 2.5 When to Use SDD vs. Manual Code

| Scenario | Approach |
|----------|----------|
| New aggregate / entity | **SDD** — full pipeline |
| New feature (CRUD, workflow) | **SDD** — full pipeline |
| New API endpoint | **SDD** — at least specify + plan |
| Bug fix in existing code | Manual edit (reference existing spec) |
| Refactoring (no behavior change) | Manual edit |
| SharedKernel primitives | Manual (infrastructure, not a feature) |
| EF migration | Manual (`dotnet ef migrations add`) |
| Config / appsettings change | Manual |

---

## 3. Project Dependency Rules

```
CliniKey.API → CliniKey.Application, CliniKey.Infrastructure
CliniKey.Infrastructure → CliniKey.Application
CliniKey.Application → CliniKey.Domain
CliniKey.Domain → CliniKey.SharedKernel
CliniKey.Tests → Domain, Application, Infrastructure
```

**Hard violations (will not compile):**
- Domain MUST NEVER reference Infrastructure or API.
- Application MUST NEVER reference Infrastructure or API.
- SharedKernel MUST NEVER reference any other project.

---

## 4. Layer Responsibilities

### SharedKernel — `src/CliniKey.SharedKernel/`
Cross-cutting DDD primitives. Zero business logic.

- `Primitives/` → `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDomainEvent`
- `Interfaces/` → `IUnitOfWork`, `IAuditableEntity`, `ISoftDeletable`
- `Enums/` → Shared enums across bounded contexts

### Domain — `src/CliniKey.Domain/`
Pure C# — no framework dependencies beyond SharedKernel.

- `Entities/` → Aggregate roots and child entities
- `ValueObjects/` → Immutable concepts (Money, PhoneNumber, ToothCode)
- `Enums/` → Domain enumerations
- `Events/` → Domain event records implementing `IDomainEvent`
- `Repositories/` → Interface contracts only (never implementations)
- `Errors/` → Static `Error` factory classes per aggregate

### Application — `src/CliniKey.Application/`
Orchestration layer. Use cases, not business rules.

- `Abstractions/` → `ICommand<T>`, `IQuery<T>`, handler interfaces
- `Features/{Feature}/Commands/` → Command + Handler + Validator
- `Features/{Feature}/Queries/` → Query + Handler
- `Features/{Feature}/Events/` → Domain event side-effect handlers
- `DTOs/` → Response DTOs (never expose domain entities)
- `Behaviors/` → MediatR pipeline (validation, logging, transactions)

### Infrastructure — `src/CliniKey.Infrastructure/`
All external concerns.

- `Persistence/` → `AppDbContext`, `UnitOfWork`
- `Persistence/Configurations/` → `IEntityTypeConfiguration<T>` per entity
- `Persistence/Repositories/` → Concrete repository implementations
- `Identity/` → ASP.NET Identity, JWT token service
- `Services/` → Email, SMS, file upload, PDF generation
- `Localization/` → `.resx` files for `en-US` (primary) and `ar-EG`

### API — `src/CliniKey.API/`
Thin HTTP adapter. No business logic.

- `Controllers/` → One per feature, delegates to MediatR `ISender`
- `Middleware/` → Exception handler, tenant resolution, request logging
- `Filters/` → Action filters (validation, etc.)

---

## 5. C# Coding Conventions

### 5.1 Compiler Settings
- `<Nullable>enable</Nullable>` — everywhere.
- `<ImplicitUsings>enable</ImplicitUsings>` — everywhere.
- File-scoped namespaces: always (`namespace X.Y.Z;`).

### 5.2 Language Features (C# 13)
- Primary constructors for DI in handlers/services.
- Collection expressions: `[]` syntax.
- Expression-bodied members for single-line methods.
- `var` when type is obvious from RHS; explicit type otherwise.

### 5.3 Naming

| Symbol | Convention | Example |
|--------|-----------|---------|
| Class / Record | PascalCase | `CreatePatientCommand` |
| Interface | `I` + PascalCase | `IPatientRepository` |
| Method | PascalCase verb | `GetByIdAsync` |
| Private field | `_camelCase` | `_patientRepository` |
| Parameter | camelCase | `patientId` |
| Constant | PascalCase | `MaxTeethCount` |
| Async method | Suffix `Async` | `CreateAsync` |
| Boolean | Prefix `Is/Has/Can` | `IsActive`, `HasInsurance` |

### 5.4 Forbidden Patterns
- ❌ `throw` for business rule violations → use `Result.Failure(error)`
- ❌ Anemic domain models → entities encapsulate behavior
- ❌ `public set` on entity properties → private/protected setters + methods
- ❌ Returning domain entities from Application layer → map to DTOs
- ❌ `DateTime.Now` / `DateTime.UtcNow` directly → inject `TimeProvider`
- ❌ Magic strings/numbers → constants or enums
- ❌ Service Locator → constructor injection only
- ❌ `async void` → always return `Task`
- ❌ Writing raw C# for new features without a spec → use SDD pipeline

---

## 6. DDD Modeling Rules

### 6.1 Entity Pattern
```csharp
public sealed class Patient : AggregateRoot<Guid>
{
    public PatientName Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    
    private Patient() { } // EF Core parameterless ctor
    
    public static Patient Create(PatientName name, PhoneNumber phone)
    {
        var patient = new Patient { Id = Guid.NewGuid(), Name = name, Phone = phone };
        patient.RaiseDomainEvent(new PatientCreatedEvent(patient.Id));
        return patient;
    }
    
    public Result UpdatePhone(PhoneNumber newPhone)
    {
        if (newPhone == Phone) return Result.Success();
        Phone = newPhone;
        MarkUpdated();
        return Result.Success();
    }
}
```

### 6.2 Value Object Pattern
```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    
    public static Result<Money> Create(decimal amount, string currency = "EGP")
    {
        if (amount < 0)
            return Result.Failure<Money>(Error.Validation("Money.Negative", "Amount cannot be negative."));
        return new Money(amount, currency);
    }
    
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 6.3 Domain Events
```csharp
public sealed record AppointmentScheduledEvent(
    Guid AppointmentId, Guid PatientId, Guid DentistId,
    DateTime OccurredOnUtc) : IDomainEvent;
```
- Immutable records. Past-tense names. Carry only IDs.

### 6.4 Domain Errors
```csharp
public static class PatientErrors
{
    public static readonly Error DuplicatePhone = 
        Error.Conflict("Patient.DuplicatePhone", "A patient with this phone already exists.");
    public static Error NotFound(Guid id) => Error.NotFound("Patient", id);
}
```

---

## 7. CQRS & MediatR

### 7.1 Abstractions
```csharp
public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
```

### 7.2 Feature Folder Layout
```
Features/{Feature}/Commands/{CommandName}/
    {CommandName}Command.cs
    {CommandName}CommandHandler.cs       ← internal sealed
    {CommandName}CommandValidator.cs     ← FluentValidation
Features/{Feature}/Queries/{QueryName}/
    {QueryName}Query.cs
    {QueryName}QueryHandler.cs          ← internal sealed
```

### 7.3 Handler Rules
- One handler per file. Handlers are `internal sealed`.
- Commands mutate state. Queries are read-only.
- Queries MAY bypass repositories for Dapper reads.
- Every handler returns `Result` or `Result<T>`.

---

## 8. API Standards

### 8.1 Controller Pattern
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class PatientsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreatePatientRequest request, CancellationToken ct)
    {
        var result = await sender.Send(request.ToCommand(), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemDetails();
    }
}
```

### 8.2 Rules
- Controllers are `sealed`. Inject `ISender` only — no repos or services.
- `CancellationToken` on every async endpoint.
- API versioning via URL: `/api/v1/`.
- RFC 7807 `ProblemDetails` for all errors.
- Request DTOs for input, response DTOs for output.

---

## 9. Error Handling Pipeline

```
Domain (Result<T>) → Handler (Result<T>) → Controller (ProblemDetails)
                                          → GlobalExceptionMiddleware (500s only)
```

- Business failures: `Result.Failure(error)` — never throw.
- Validation: FluentValidation via `ValidationBehavior<T>` — returns 400.
- Unhandled exceptions: `GlobalExceptionMiddleware` — logs + generic 500.

---

## 10. Persistence (EF Core + Dapper)

- One `IEntityTypeConfiguration<T>` per entity. Fluent API only.
- Value objects via `OwnsOne` or `ValueConverter<T>`.
- All strings: explicit `MaxLength`. No `nvarchar(max)`.
- Repositories on aggregate roots only. No `IQueryable<T>` leaking.
- `AsNoTracking()` for all read queries.
- Complex reads: Dapper via `IDbConnection` in query handlers.

---

## 11. Multi-Tenancy

- Strategy: schema-per-tenant in shared PostgreSQL server.
- Resolution: `X-Tenant-Id` header or JWT claim via middleware.
- DbContext: sets `search_path` per tenant on `OnConfiguring`.
- Every query MUST be tenant-scoped. No cross-tenant data leakage.

---

## 12. Localization (English-First + Arabic)

- **Primary locale**: `en-US`. **Secondary**: `ar-EG`.
- All user-facing strings: English by default in code.
- Resource files: `Infrastructure/Localization/Resources/{Feature}.ar-EG.resx`.
- API error messages: localized via `IStringLocalizer<T>`.
- Bilingual DB columns: `LocalizedString` value object with `En` and `Ar` properties.
- API returns `Content-Language` header. UI direction (RTL) is frontend concern.
- Currency: always **EGP**. Display format follows request culture.

---

## 13. Egyptian Market Rules

- Invoices in **EGP** (Egyptian Pound).
- Tax: **14% VAT** — stored per invoice line for audit trail.
- Payments: Cash, Visa, InstaPay, Fawry, insurance claim.
- Tooth numbering: **FDI (ISO 3950)** two-digit system.
- Treatment codes: internal catalog, mappable to insurance provider codes.
- Insurance: store policy number, provider name, coverage percentage.

---

## 14. Testing

- Framework: **xUnit**. Assertions: **FluentAssertions**. Mocking: **NSubstitute**.
- Integration tests: **Testcontainers** (PostgreSQL).
- API tests: `WebApplicationFactory`.
- Naming: `MethodUnderTest_Scenario_ExpectedResult`.
- Domain tests MUST NEVER touch a database.
- Structure: `tests/CliniKey.Tests/{Domain,Application,Infrastructure,API}/`.

---

## 15. Security

- Passwords: ASP.NET Identity default (PBKDF2).
- JWT: 15-min access tokens + 7-day rotating refresh tokens.
- Roles: `Admin`, `Dentist`, `Receptionist` + resource-based policies.
- Tenant isolation enforced on every query.
- FluentValidation on every command. Never trust client input.
- PII encrypted at rest. CORS: explicit origin allowlist, no `*`.

---

## 16. Approved NuGet Packages

| Package | Layer | Purpose |
|---------|-------|---------|
| MediatR | SharedKernel, Application | CQRS + events |
| FluentValidation | Application | Command validation |
| Npgsql.EntityFrameworkCore.PostgreSQL | Infrastructure | PostgreSQL |
| Microsoft.EntityFrameworkCore | Infrastructure | ORM |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Infrastructure | Auth |
| Dapper | Infrastructure | Read queries |
| Serilog + Sinks.Console | API | Structured logging |
| Mapperly | Application | Compile-time mapping |
| xUnit | Tests | Test framework |
| FluentAssertions | Tests | Assertions |
| NSubstitute | Tests | Mocking |
| Testcontainers | Tests | DB integration |

Adding unlisted packages requires explicit justification.

---

## 17. Git Standards

- Branches: `feature/CK-{ticket}-slug`, `fix/CK-{ticket}-slug`
- Commits: Conventional Commits — `feat:`, `fix:`, `chore:`, `refactor:`, `docs:`, `test:`
- Every PR must include at least one test.

---

## 18. File Rules

- One type per file. File name = type name.
- No `Helpers`, `Utils`, `Misc` folders.
- Max line length: 120 chars (soft).
- Empty scaffold folders: keep with `.gitkeep` if needed.

---

> **End of standards.** This document governs all code generation.
> Update only through explicit agreement.
