---
name: clinikey-architecture-standards
description: Authoritative CliniKey coding, review, and fix standards for .NET 10/C# 13 Clean Architecture, DDD, CQRS, EF Core, Dapper, Identity, JWT auth, tenant isolation, Result handling, TimeProvider usage, controllers, authorization, migrations, and tests. Use whenever Codex reviews, edits, implements, refactors, or generates code in the CliniKey solution.
---

# CliniKey Architecture Standards

Read and apply these standards before reviewing, changing, or generating CliniKey code.

## Stack

- Use .NET 10 / C# 13.
- Preserve Clean Architecture dependency flow: Domain -> Application -> Infrastructure -> API.
- Model with DDD: aggregates, value objects, domain events, and explicit errors.
- Use CQRS via MediatR: commands through EF Core + domain model; queries through Dapper + DTOs.
- Use PostgreSQL, ASP.NET Core Identity, and JWT authentication.

## Project Structure

- `CliniKey.Domain`: entities, value objects, domain events, errors.
- `CliniKey.Application`: commands, queries, handlers, validators, behaviors.
- `CliniKey.Infrastructure`: EF Core, Dapper, Identity, auth, persistence.
- `CliniKey.API`: controllers, middleware, extensions.
- `CliniKey.SharedKernel`: Result, Error, AggregateRoot, Entity, interfaces.
- `CliniKey.Tests`: unit and integration tests.

## Result Pattern

- Return `Result` or `Result<T>` for every expected failure.
- Never throw for expected failures.
- Ensure `Result` and `Result<T>` both implement `IResult`.
- Use these HTTP mappings:
  - `ErrorType.Validation` -> 400
  - `ErrorType.NotFound` -> 404
  - `ErrorType.Conflict` -> 409
  - `ErrorType.Unauthorized` -> 401
  - `ErrorType.Forbidden` -> 403
- Keep all mappings in `ResultExtensions.ToActionResult()`.
- Do not hard-code status-code mapping in controllers.
- Implement `ValidationBehavior` without reflection; use static abstract interface members.

## TimeProvider

- Do not use `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, or `DateTime.Now`.
- Use `Clock.GetUtcNow().UtcDateTime` in aggregates.
- Use injected `TimeProvider` in services, validators, and behaviors.
- Register `TimeProvider.System` as a singleton.
- Capture time once per method:

```csharp
var now = Clock.GetUtcNow().UtcDateTime;
```

## AggregateRoot

Use this shape:

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
{
    protected readonly TimeProvider Clock;
    public DateTime CreatedAtUtc { get; protected init; }
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected AggregateRoot() { }

    protected AggregateRoot(TimeProvider clock)
    {
        Clock = clock;
        CreatedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    protected void MarkUpdated()
        => UpdatedAtUtc = Clock.GetUtcNow().UtcDateTime;
}
```

- Subclasses must call `base(clock)` in parameterized constructors.
- Subclasses must not store a separate `_clock` field.
- Do not call domain methods from EF-only parameterless constructors.
- Keep `CreatedAtUtc` as `protected init`.
- Keep `UpdatedAtUtc` as `protected set`; write it only through `MarkUpdated()`.

## Entity

Use a non-generic `IHasDomainEvents` for the UnitOfWork change tracker:

```csharp
public abstract class Entity<TId> : IHasDomainEvents
    where TId : notnull
{
    public TId Id { get; protected init; }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

## Join Entities

- Keep `ClinicDentist` as `Entity<Guid>`, not `AggregateRoot`.
- Join entities have no `TimeProvider`, no domain events, no `Result` factory, and no length constants.
- Do not apply aggregate conventions to join entities.

## Auditing

Use this interface:

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    DateTime? UpdatedAtUtc { get; }
}
```

- Update audit fields through EF Core metadata API, never direct property assignment:

```csharp
entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).CurrentValue = now;
entry.Property(nameof(IAuditableEntity.UpdatedAtUtc)).CurrentValue = now;
```

## Factories

- Use private constructors plus static factories as the only creation path.
- Factory methods always return `Result<T>`.
- Parameterless private constructors are for EF Core hydration only.
- Every aggregate creation factory raises a created domain event.
- Never throw from factory methods for expected validation.

## String Validation

Every string property must have:

- A `public const int` on the class for max length.
- Validation in both `Create` and every update method.
- Idempotency check first in update methods.
- EF configuration that references the constant, never a magic number.

Example:

```csharp
public Result UpdateFullName(string fullName)
{
    if (FullName == fullName) return Result.Success();
    if (string.IsNullOrWhiteSpace(fullName)) return Result.Failure(Errors.Empty);
    if (fullName.Length > MaxFullNameLength) return Result.Failure(Errors.TooLong);

    FullName = fullName;
    MarkUpdated();
    return Result.Success();
}
```

## Immutability

- Make `Clinic.SchemaName` `private init`; it maps to a PostgreSQL schema and must not change after creation.
- Use `private init` for any property whose later mutation would corrupt data integrity.

## Domain Events

- Raise events inside domain methods through `RaiseDomainEvent()`.
- Publish events from `UnitOfWork` after `SaveChangesAsync` through `IPublisher`.
- Raise a created event from every aggregate factory.
- Raise a state-changed event for every meaningful state change.
- Capture event timestamps once as `var now = Clock.GetUtcNow().UtcDateTime`.

## Controllers

- Put top-level `[Authorize]` on controllers.
- Opt out with `[AllowAnonymous]` only for login, refresh token, and public registration.
- Keep controllers thin and business-logic-free.
- Return `result.ToActionResult()` instead of hard-coded mappings.
- Use `CreatedAtAction` with route values for POST endpoints.

```csharp
return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
```

## Authorization

- Use policy-based authorization, not raw role strings.
- Define policies in `DependencyInjection.cs` in the Application layer.
- Use endpoint attributes like `[Authorize(Policy = Policies.CanInviteStaff)]`.
- Use `ClinicAdmin` as the role name; do not use `Admin`.
- Seed roles through `RoleManager` at startup; do not use `HasData()`.

## Tenant Isolation

- Every request must carry `X-Tenant-Id` for MVP or a JWT tenant claim for production.
- `TenantResolutionMiddleware` must return 400 `ProblemDetails` if tenant resolution fails.
- Do not call `next()` without a resolved tenant.
- `ICurrentUserService.TenantId` reads from `context.Items`.
- Scope all repositories by tenant through EF Core global query filters:

```csharp
builder.HasQueryFilter(p =>
    p.TenantId == _currentUserService.TenantId && !p.IsDeleted);
```

## AuthService

- Check every `AddToRoleAsync` result and propagate failures as `Result.Failure`.
- Login must verify `clinic.IsActive` before issuing tokens.
- Refresh token rotation must use compare-and-swap to prevent concurrent reuse.
- Enforce one active refresh operation per token.
- Use a database-level unique constraint on active tokens per family.

## MediatR Pipeline

Register behaviors in this order:

```text
LoggingBehavior -> ValidationBehavior -> TransactionBehavior -> Handler
```

- `LoggingBehavior`: timing, success/failure distinction, exception logging.
- `ValidationBehavior`: FluentValidation, combined validation errors.
- `TransactionBehavior`: wraps handlers in a database transaction.
- Register all three in `DependencyInjection.cs`.

## EF Core

- Use one `IEntityTypeConfiguration<T>` per entity.
- Do not use data annotations on domain classes.
- Use `ValueGeneratedNever()` on domain-generated IDs.
- Add `HasQueryFilter` for soft delete and tenant isolation on every tenant-scoped entity.
- Use `ApplyConfigurationsFromAssembly` in `OnModelCreating`.
- Name migrations descriptively in past tense, for example `AddRefreshTokenExpiryColumn`.

## Testing

- Use `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`.
- Use a fixed time in every test class.
- Assert on `_fixedTime.UtcDateTime`.
- Never use `DateTime.UtcNow` in assertions.
- Use NSubstitute for mocking.
- Add dedicated unit tests for every aggregate domain validation rule.
