# Code Reading Guide: Tenant Provisioning

This guide is a practical study plan. Follow it when you want to understand the
feature line by line without feeling like the whole codebase is attacking you at
once.

## Before You Start

Use this mindset:

> I am not trying to read files. I am trying to understand flows.

The same feature appears in several layers. If you read by folder alphabetically,
the architecture will feel random. If you read by flow, the design starts to make
sense.

## Study Pass 1: The Domain Story

Start with:

- [Clinic.cs](../../src/CliniKey.Domain/Entities/Clinic.cs)
- [ClinicErrors.cs](../../src/CliniKey.Domain/Errors/ClinicErrors.cs)
- [TenantErrors.cs](../../src/CliniKey.Domain/Errors/TenantErrors.cs)
- [ClinicTests.cs](../../tests/CliniKey.Tests/Domain/ClinicTests.cs)

Ask these questions while reading:

| Question | What you are learning |
| --- | --- |
| Why is `SchemaName` `private init`? | Some data must be immutable after creation |
| Why does `Create` return `Result<Clinic>`? | Expected failures are part of the API |
| Why are state changes methods? | Transitions need rules, timestamps, and events |
| Why is `TimeProvider` used? | Time should be deterministic in tests |
| Why are max lengths constants? | Domain validation and EF mapping must agree |

### Mini Exercise

Trace what happens when `Deactivate` is called:

1. What state prevents the operation?
2. Which fields change?
3. Which timestamp is captured?
4. Which domain event is raised?
5. Which test proves this?

If you can answer that, you are reading the aggregate correctly.

## Study Pass 2: The Use Case

Read:

- [OnboardClinicCommand.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommand.cs)
- [OnboardClinicCommandValidator.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandValidator.cs)
- [OnboardClinicCommandHandler.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandHandler.cs)
- [OnboardClinicCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/OnboardClinicCommandHandlerTests.cs)

Focus on orchestration.

The handler is not "business rules only" and not "database details". It is the
application story:

```text
input -> validation -> duplicate check -> aggregate creation -> save -> provision -> finalize
```

### What To Notice

The handler depends on interfaces:

- `IClinicRepository`
- `ITenantProvisioningService`
- `ICurrentUserService`
- `IUnitOfWork`
- `TimeProvider`

That means the handler can be tested without PostgreSQL and without ASP.NET.

### Mini Exercise

Pretend provisioning fails.

Find the exact lines where:

1. The failure comes back from infrastructure.
2. The clinic is removed from the repository.
3. Unit of work saves the removal.
4. The failure returns to the API.

This teaches you compensation flow, which is one of the biggest production
differences from tutorial code.

## Study Pass 3: Infrastructure Reality

Read:

- [TenantProvisioningService.cs](../../src/CliniKey.Infrastructure/Persistence/TenantProvisioningService.cs)
- [TenantMigrationService.cs](../../src/CliniKey.Infrastructure/Persistence/TenantMigrationService.cs)
- [PostgresIdentifier.cs](../../src/CliniKey.Infrastructure/Persistence/PostgresIdentifier.cs)
- [TenantProvisioningIntegrationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs)
- [TenantMigrationServiceTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantMigrationServiceTests.cs)

### What To Notice

Infrastructure code is allowed to know PostgreSQL details:

- `NpgsqlConnection`
- `CREATE SCHEMA`
- `DROP SCHEMA`
- `pg_advisory_xact_lock`
- `__EFMigrationsHistory`
- schema-qualified SQL

The Domain and Application layers should not know these details.

### Mini Exercise

Read `TenantProvisioningService.ProvisionAsync` and make a failure table:

| Failure point | Cleanup attempted | Error returned |
| --- | --- | --- |
| schema creation exception | drop schema | provisioning failed |
| migration failure | drop schema | provisioning failed |
| audit log failure | inspect behavior | inspect behavior |

The goal is to see how production code thinks about partial completion.

## Study Pass 4: Request Tenant Resolution

Read:

- [TenantResolutionMiddleware.cs](../../src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs)
- [TenantRegistry.cs](../../src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs)
- [TenantContext.cs](../../src/CliniKey.Infrastructure/Persistence/TenantContext.cs)
- [TenantResolutionMiddlewareTests.cs](../../tests/CliniKey.Tests/API/TenantResolutionMiddlewareTests.cs)

### The Flow

```text
authenticated request
  -> read tenant_id claim
  -> resolve tenant in shared.clinics
  -> reject inactive or unhealthy tenant
  -> store schema in TenantContext
  -> continue request
```

### What To Notice

The middleware returns before the handler if:

- the user is unauthenticated
- the tenant claim is missing or invalid
- the tenant does not exist
- the tenant is inactive
- the schema is unhealthy

This is defensive design. The goal is to prevent invalid requests from reaching
business handlers at all.

### Mini Exercise

Pick one failure case from the middleware tests. Then find:

1. The mocked registry result.
2. The expected HTTP status code.
3. The `ProblemDetails` title/detail.
4. Whether `next` was called.

If `next` is called on an invalid tenant, tenant isolation is broken.

## Study Pass 5: Search Path And Data Access

Read:

- [TenantConnectionInterceptor.cs](../../src/CliniKey.Infrastructure/Persistence/TenantConnectionInterceptor.cs)
- [DbConnectionFactory.cs](../../src/CliniKey.Infrastructure/Persistence/DbConnectionFactory.cs)
- [IDbConnectionFactory.cs](../../src/CliniKey.Application/Abstractions/Data/IDbConnectionFactory.cs)
- One Dapper query handler, for example:
  - [ListPatientsQueryHandler.cs](../../src/CliniKey.Application/Features/Patients/Queries/ListPatients/ListPatientsQueryHandler.cs)
- [TenantSchemaSwitchingTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs)
- [TenantDapperConnectionTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantDapperConnectionTests.cs)
- [TenantConcurrentIsolationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs)

### The Crucial Idea

Search path is connection state. Connection state is dangerous because connections
are pooled.

That is why the app sets search path each time it opens a tenant connection.

### EF Core Path

```text
AppDbContext opens connection
  -> TenantConnectionInterceptor runs
  -> SET search_path TO tenant_x, shared, public
  -> EF query executes
```

### Dapper Path

```text
Query handler asks for CreateTenantConnection
  -> DbConnectionFactory checks TenantContext
  -> opens NpgsqlConnection
  -> SET search_path TO tenant_x, shared, public
  -> Dapper query executes
```

### Mini Exercise

Search for:

```text
CreateConnection()
```

and:

```text
CreateTenantConnection()
```

For every query handler, decide whether it should read shared data or tenant data.
Tenant data should use `CreateTenantConnection`.

This is exactly the kind of review habit senior engineers build.

## Study Pass 6: Shared Schema

Read:

- [SharedDbContext.cs](../../src/CliniKey.Infrastructure/Persistence/SharedDbContext.cs)
- [ClinicConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/ClinicConfiguration.cs)
- [DentistConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/DentistConfiguration.cs)
- [ClinicDentistConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/ClinicDentistConfiguration.cs)
- [SharedSchemaMappingTests.cs](../../tests/CliniKey.Tests/Infrastructure/SharedSchemaMappingTests.cs)
- [CrossTenantDentistQueryTests.cs](../../tests/CliniKey.Tests/Infrastructure/CrossTenantDentistQueryTests.cs)

### What To Notice

`Clinic`, `Dentist`, and `ClinicDentist` are cross-tenant registry concepts. They
must remain reachable even when the current connection's search path points at a
tenant schema.

That is why they are mapped to `shared`.

If these mappings were ambiguous, a tenant search path could accidentally create or
query the wrong table.

## Study Pass 7: Lifecycle And Operations

Read:

- [ActivateClinic command folder](../../src/CliniKey.Application/Features/Tenants/Commands/ActivateClinic/)
- [DeactivateClinic command folder](../../src/CliniKey.Application/Features/Tenants/Commands/DeactivateClinic/)
- [MigrateTenantSchemas command folder](../../src/CliniKey.Application/Features/Tenants/Commands/MigrateTenantSchemas/)
- [GetTenantSchemaHealth query folder](../../src/CliniKey.Application/Features/Tenants/Queries/GetTenantSchemaHealth/)
- [ClinicLifecycleCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/ClinicLifecycleCommandHandlerTests.cs)
- [TenantLifecycleAccessTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantLifecycleAccessTests.cs)

### What To Notice

The platform does not delete tenant data when a clinic is deactivated. It blocks
access by changing lifecycle state.

That is a production-grade data decision:

- preserve history
- avoid destructive admin actions
- allow reactivation
- keep auditability

## Important Terms

| Term | Meaning in this feature |
| --- | --- |
| Tenant | A clinic using the system with isolated operational data |
| Control plane | Platform-level management of tenants |
| Data plane | Normal tenant-scoped application behavior |
| Shared schema | Cross-tenant registry and relationship data |
| Tenant schema | Per-clinic operational data schema |
| Search path | PostgreSQL setting controlling which schema unqualified table names use |
| Provisioning | Creating tenant schema and applying baseline migrations |
| Compensation | Cleanup performed after a partial failure |
| Registry | Shared lookup from tenant ID to schema/status/health |
| Health status | Whether a tenant schema is safe to serve |

## Common Junior Misreadings

### "Why not just put all this in the controller?"

Because controllers are hard to reuse, hard to test deeply, and too close to HTTP.
The tenant onboarding story is not an HTTP concept. It is an application use case.

### "Why do we need interfaces if we only have one implementation?"

Interfaces here are not for imaginary future databases. They protect architectural
direction. Application can express what it needs without depending on Npgsql,
EF Core, or PostgreSQL.

### "Why not throw exceptions for invalid clinic data?"

Invalid clinic data is expected user input. Expected outcomes should be represented
as `Result` values so the caller must handle them.

### "Why do tests use different levels?"

Because each level catches different bugs:

- Domain tests catch rule mistakes.
- Handler tests catch orchestration mistakes.
- API tests catch HTTP and middleware mistakes.
- Integration tests catch database reality.

No single test style covers all of that well.

### "Why is Infrastructure so big?"

Because external systems are detailed. PostgreSQL does not disappear just because
the architecture diagram is clean. Infrastructure is where those details are
contained so the rest of the system can stay understandable.

## Red Flags To Watch For In Future Work

When you or AI add new code, be suspicious if you see:

- A tenant-scoped Dapper query using `CreateConnection`.
- A controller manually mapping business errors to status codes.
- A domain entity accepting a schema name from user input.
- `DateTime.UtcNow` inside domain or application code.
- Direct property mutation for clinic lifecycle state.
- SQL string interpolation with unquoted identifiers.
- New tenant routes added without deciding control-plane vs data-plane.
- Cache invalidation missing after status or health changes.
- Tests that only prove the happy path.

These are not style nits. They are places where production bugs are born.

## A Staff Engineer's Reading Advice

When you study AI-generated code, do not ask only:

> Is this code correct?

Ask:

> What responsibility is this code claiming?

Then ask:

> Is this the right place for that responsibility?

That is the difference between understanding syntax and understanding architecture.

## Final Study Challenge

After reading the feature, write your own short answer to each prompt:

1. Why is tenant resolution middleware instead of a helper method in each controller?
2. Why does Dapper need a tenant-aware connection factory if EF Core already has an interceptor?
3. Why should `SchemaName` never change after clinic creation?
4. What would go wrong if inactive tenants were filtered only inside query handlers?
5. What is the difference between tenant provisioning status and schema health status?
6. Which tests would fail if search path were not set on every connection?
7. Where would you add observability for slow tenant provisioning?
8. Which layer should own a future "rename clinic" feature?
9. What would be risky about allowing platform operators to choose schema names?
10. What quickstart steps still need manual validation before release confidence is complete?

If you can answer these without looking at the files, you are no longer just reading
the project. You are starting to think inside it.
