# Code Reading Guide: Tenant Provisioning

Use this guide when you want to study the implementation without reading files in
alphabetical order. The feature is best understood as flows.

## Before You Start

Hold this model in your head:

```text
Tenant = practice isolation boundary
Clinic = branch/location under a tenant
```

If a field decides schema access, provisioning, health, or migration state, it
belongs to `Tenant`. If a field describes a branch's contact information or local
status, it belongs to `Clinic`.

## Study Pass 1: Domain Boundary

Read:

- [Tenant.cs](../../src/CliniKey.Domain/Entities/Tenant.cs)
- [Clinic.cs](../../src/CliniKey.Domain/Entities/Clinic.cs)
- [TenantErrors.cs](../../src/CliniKey.Domain/Errors/TenantErrors.cs)
- [ClinicErrors.cs](../../src/CliniKey.Domain/Errors/ClinicErrors.cs)
- [TenantTests.cs](../../tests/CliniKey.Tests/Domain/TenantTests.cs)
- [ClinicTests.cs](../../tests/CliniKey.Tests/Domain/ClinicTests.cs)

Ask:

| Question | What you are learning |
| --- | --- |
| Why does `Tenant` own `SchemaName`? | Isolation belongs to the practice, not a branch |
| Why does `Clinic` have `TenantId`? | A branch is attached to a practice |
| Why does `Tenant.Create` raise `TenantCreatedEvent`? | Aggregate creation should be visible to domain event flows |
| Why are provisioning and health separate? | A tenant can exist before it is safe to serve |
| Why do both aggregates still have lifecycle methods? | Tenant access and branch status are related but not identical |

Mini exercise: explain why `CurrentMigration` on `Clinic` would be wrong once a
practice has two branches.

## Study Pass 2: Onboarding Use Case

Read:

- [OnboardClinicCommand.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommand.cs)
- [OnboardClinicCommandValidator.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandValidator.cs)
- [OnboardClinicCommandHandler.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandHandler.cs)
- [OnboardClinicResponse.cs](../../src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicResponse.cs)
- [OnboardClinicCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/OnboardClinicCommandHandlerTests.cs)

Trace this sequence:

```text
phone validation
  -> duplicate branch phone check
  -> tenant id + clinic id generation
  -> schema name generation from tenant id
  -> Tenant.Create
  -> Clinic.Create
  -> tenant.MarkProvisioning
  -> save shared rows
  -> provision schema
  -> tenant.MarkProvisioned
  -> save final tenant state
```

Mini exercise: find exactly where a provisioning failure removes both the clinic
and the tenant registry row.

## Study Pass 3: Provisioning Infrastructure

Read:

- [TenantProvisioningService.cs](../../src/CliniKey.Infrastructure/Persistence/TenantProvisioningService.cs)
- [TenantMigrationService.cs](../../src/CliniKey.Infrastructure/Persistence/TenantMigrationService.cs)
- [PostgresIdentifier.cs](../../src/CliniKey.Infrastructure/Persistence/PostgresIdentifier.cs)
- [TenantProvisioningIntegrationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs)
- [TenantMigrationServiceTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantMigrationServiceTests.cs)

Look for:

- advisory lock acquisition
- schema identifier quoting
- `CREATE SCHEMA IF NOT EXISTS`
- baseline operational tables
- `__EFMigrationsHistory`
- schema drop compensation
- audit log writes

Mini exercise: make a table with three rows: schema creation failure, migration
failure, and audit failure. For each, write what cleanup the current code attempts.

## Study Pass 4: Shared Registry And Mappings

Read:

- [SharedDbContext.cs](../../src/CliniKey.Infrastructure/Persistence/SharedDbContext.cs)
- [AppDbContext.cs](../../src/CliniKey.Infrastructure/Persistence/AppDbContext.cs)
- [TenantConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/TenantConfiguration.cs)
- [ClinicConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/ClinicConfiguration.cs)
- [TenantRepository.cs](../../src/CliniKey.Infrastructure/Persistence/Repositories/TenantRepository.cs)
- [ClinicRepository.cs](../../src/CliniKey.Infrastructure/Persistence/Repositories/ClinicRepository.cs)
- [SharedSchemaMappingTests.cs](../../tests/CliniKey.Tests/Infrastructure/SharedSchemaMappingTests.cs)

Ask:

| Question | Why it matters |
| --- | --- |
| Why does `AppDbContext` exclude shared tables from tenant migrations? | Tenant schemas should not create registry tables |
| Why does `SharedDbContext` seed a dev tenant and clinic? | Local development needs a known registry shape |
| Why does `TenantRepository.ListAsync` optionally require clinics? | Clinic-list pagination should not count tenant rows without branches |
| Why are phone uniqueness checks on `ClinicRepository`? | Phone belongs to branch contact data |

Mini exercise: follow the `Tenant` to `Clinic` relationship from configuration to
repository query.

## Study Pass 5: Request Resolution

Read:

- [TenantResolutionMiddleware.cs](../../src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs)
- [TenantRegistry.cs](../../src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs)
- [ITenantRegistry.cs](../../src/CliniKey.Application/Abstractions/Tenancy/ITenantRegistry.cs)
- [TenantContext.cs](../../src/CliniKey.Infrastructure/Persistence/TenantContext.cs)
- [TenantResolutionMiddlewareTests.cs](../../tests/CliniKey.Tests/API/TenantResolutionMiddlewareTests.cs)
- [TenantLifecycleAccessTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantLifecycleAccessTests.cs)

The registry gate should reject:

- missing tenant row
- inactive tenant
- not-provisioned tenant
- unhealthy schema

Mini exercise: explain why `TenantStatus.Active` is not enough without
`TenantProvisioningStatus.Provisioned`.

## Study Pass 6: Search Path And Tenant Data Access

Read:

- [TenantConnectionInterceptor.cs](../../src/CliniKey.Infrastructure/Persistence/TenantConnectionInterceptor.cs)
- [DbConnectionFactory.cs](../../src/CliniKey.Infrastructure/Persistence/DbConnectionFactory.cs)
- [IDbConnectionFactory.cs](../../src/CliniKey.Application/Abstractions/Data/IDbConnectionFactory.cs)
- [TenantSchemaSwitchingTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs)
- [TenantDapperConnectionTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantDapperConnectionTests.cs)
- [TenantConcurrentIsolationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs)

The key idea:

```text
search_path is connection state
connection state is pooled
therefore every tenant connection must be prepared on open
```

Mini exercise: find a Dapper query handler for tenant operational data and verify
it uses a tenant-aware connection path.

## Study Pass 7: Auth And Identity

Read:

- [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs)
- [ApplicationUser.cs](../../src/CliniKey.Infrastructure/Identity/ApplicationUser.cs)
- [JwtTokenService.cs](../../src/CliniKey.Infrastructure/Identity/JwtTokenService.cs)
- [CurrentUserService.cs](../../src/CliniKey.Infrastructure/Identity/CurrentUserService.cs)

What changed conceptually:

- Registration still accepts a clinic ID.
- Auth loads the clinic, then stores the owning tenant ID on the user.
- JWTs carry `tenant_id`.
- Middleware resolves the tenant from that claim.

Mini exercise: explain why storing `clinicId` as the user tenant claim would break
after the tenant/practice refactor.

## Study Pass 8: Day-Two Operations

Read:

- [ActivateClinicCommandHandler.cs](../../src/CliniKey.Application/Features/Tenants/Commands/ActivateClinic/ActivateClinicCommandHandler.cs)
- [DeactivateClinicCommandHandler.cs](../../src/CliniKey.Application/Features/Tenants/Commands/DeactivateClinic/DeactivateClinicCommandHandler.cs)
- [MigrateTenantSchemasCommandHandler.cs](../../src/CliniKey.Application/Features/Tenants/Commands/MigrateTenantSchemas/MigrateTenantSchemasCommandHandler.cs)
- [GetTenantSchemaHealthQueryHandler.cs](../../src/CliniKey.Application/Features/Tenants/Queries/GetTenantSchemaHealth/GetTenantSchemaHealthQueryHandler.cs)
- [ClinicLifecycleCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/ClinicLifecycleCommandHandlerTests.cs)
- [TenantMigrationCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/TenantMigrationCommandHandlerTests.cs)

Notice that the lifecycle endpoint still receives `clinicId` for API continuity,
then acts on the owning tenant. That is a V1 compatibility choice. Future
multi-branch work should revisit naming and semantics.

## Important Terms

| Term | Meaning |
| --- | --- |
| Tenant | Practice isolation boundary |
| Clinic | Branch/location under a tenant |
| Control plane | Platform operations over shared registry data |
| Data plane | Tenant-scoped operational app behavior |
| Provisioning status | Whether schema creation/migration workflow has completed |
| Schema health | Whether the tenant schema is safe to serve |
| Current migration | The latest migration known for a tenant schema |
| Search path | PostgreSQL setting that chooses which schema unqualified tables hit |
| Compensation | Cleanup after partial provisioning failure |

## Common Misreadings

### "The route says clinic, so clinic must own the schema."

The route is V1 product language. The model is tenant-first because practices can
grow beyond one branch.

### "Active tenant means ready tenant."

Not enough. A tenant can be active but not provisioned or not healthy. Request
resolution checks all three ideas.

### "Shared data is less sensitive than tenant data."

Shared registry data decides tenant access. Bad mappings or stale cache here can
be as dangerous as a bad tenant query.

### "Search path is set once and done."

No. Pooled connections make that unsafe. Set it when opening tenant-aware
connections.

## Red Flags For Future Work

- Adding schema/provisioning fields back onto `Clinic`.
- Treating `clinicId` and `tenantId` as interchangeable.
- New tenant-scoped Dapper queries using a non-tenant connection.
- New shared entities without explicit shared-schema mapping.
- Tenant lifecycle changes without registry cache invalidation.
- API responses that expose `Status` without clarifying branch status vs tenant status.
- Tests that create a tenant but forget to mark it provisioned before resolution.

## Final Study Challenge

Answer these from memory:

1. Why does V1 onboarding create both `Tenant` and `Clinic`?
2. Which aggregate owns `SchemaName`, and why?
3. What three tenant states must pass before middleware resolves a request?
4. Why does registration accept `clinicId` but store `TenantId`?
5. What gets created in `shared`, and what gets created in `tenant_*`?
6. Which code attempts cleanup after provisioning failure?
7. Why do EF Core and Dapper each need tenant-aware connection setup?
8. Which future product feature will most pressure the current endpoint naming?
