# Tenant Boundary, Options, And Infrastructure Fixes

This note records the infrastructure lessons around feature `003-tenant-provisioning`.
It started as a post-review explanation of options and DI fixes. The current
version also reflects the larger correction: tenant/practice is now the isolation
boundary, and clinic is a branch under that boundary.

## The Problems We Solved

| Problem | Why it mattered | Current fix |
| --- | --- | --- |
| Clinic owned schema/provisioning/health | Multi-branch practices would not have a clear isolation owner | `Tenant` owns schema name, provisioning status, health, and current migration |
| User auth stored the submitted clinic ID as tenant claim | Claims would point at a branch, not the practice boundary | Registration maps `clinicId` to `clinic.TenantId` |
| Registry resolution read clinic rows | Access control belonged to the wrong table | `TenantRegistry` reads `shared.tenants` |
| Provisioning status was not part of resolution | Active and healthy alone did not prove provisioning had completed | Registry validates `ProvisioningStatus == Provisioned` |
| Shared schema and tenant prefix options were easy to bypass | Operators need configuration that actually affects runtime behavior | Services consume `TenancyOptions` through `IOptions<T>` |
| EF model cache did not include tenancy schema options | EF could reuse a model built for a different shared schema | Tenancy-aware model cache key preserves mapping correctness |
| Services manually constructed dependencies in DI | Lifetimes and options became harder to review | Constructor injection and normal scoped registrations are used |

## Tenant Boundary Correction

Before the refactor, the code treated `Clinic` as both the customer-facing clinic
and the infrastructure tenant. That is acceptable only while every practice has
exactly one location forever.

The corrected model is:

```text
Tenant / Practice
  owns schema, provisioning, health, migration, lifecycle access

Clinic
  owns branch name, phone, address, branch status
```

Start with:

- [Tenant.cs](../../src/CliniKey.Domain/Entities/Tenant.cs)
- [Clinic.cs](../../src/CliniKey.Domain/Entities/Clinic.cs)
- [TenantConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/TenantConfiguration.cs)
- [ClinicConfiguration.cs](../../src/CliniKey.Infrastructure/Persistence/Configurations/ClinicConfiguration.cs)

The useful review heuristic is:

> If a field controls database isolation or whole-practice access, it belongs on `Tenant`.

## Options Became Real Runtime Inputs

Tenancy configuration is not decoration. `SharedSchema`, `TenantSchemaPrefix`,
cache duration, and lock key influence data placement and runtime safety.

Important files:

- [TenancyOptions.cs](../../src/CliniKey.Infrastructure/Persistence/TenancyOptions.cs)
- [TenantSchemaNameGenerator.cs](../../src/CliniKey.Infrastructure/Persistence/TenantSchemaNameGenerator.cs)
- [DependencyInjection.cs](../../src/CliniKey.Infrastructure/DependencyInjection.cs)
- [TenancyModelCacheKeyFactory.cs](../../src/CliniKey.Infrastructure/Persistence/TenancyModelCacheKeyFactory.cs)

The domain validates a schema name, but infrastructure decides how the schema name
is generated from configured options. That keeps configuration out of the domain.

## Why EF Model Cache Matters

EF caches models. If the shared schema is configurable, the model cache key must
include the schema choice. Otherwise a model built for one shared schema could be
reused under another option value.

That is why the infrastructure uses a tenancy-aware model cache key for contexts
that depend on shared-schema configuration.

## Registry Resolution Now Has Four Checks

[TenantRegistry.cs](../../src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs)
loads:

- tenant ID
- schema name
- tenant lifecycle status
- provisioning status
- schema health status
- current migration

Then it validates:

```text
tenant exists
TenantStatus == Active
TenantProvisioningStatus == Provisioned
TenantSchemaHealthStatus == Healthy
```

This is a good example of access-control thinking. The registry is not just a
cache; it is the gatekeeper that decides whether tenant-scoped handlers may run.

## Shared Versus Tenant Migrations

Shared migrations create platform registry tables:

- [20260523090312_AddSharedTenantRegistry.cs](../../src/CliniKey.Infrastructure/Persistence/Migrations/Shared/20260523090312_AddSharedTenantRegistry.cs)

Tenant migrations represent operational tables:

- [20260523090321_InitialTenantOperationalSchema.cs](../../src/CliniKey.Infrastructure/Persistence/Migrations/Tenant/20260523090321_InitialTenantOperationalSchema.cs)

[AppDbContext.cs](../../src/CliniKey.Infrastructure/Persistence/AppDbContext.cs)
explicitly excludes shared registry tables from tenant migrations. That prevents
new tenant schemas from accidentally creating their own copies of `tenants` or
`clinics`.

## DI And Lifetimes

The current shape registers repository and tenancy services through DI:

- [TenantRepository.cs](../../src/CliniKey.Infrastructure/Persistence/Repositories/TenantRepository.cs)
- [ClinicRepository.cs](../../src/CliniKey.Infrastructure/Persistence/Repositories/ClinicRepository.cs)
- [TenantProvisioningService.cs](../../src/CliniKey.Infrastructure/Persistence/TenantProvisioningService.cs)
- [TenantRegistry.cs](../../src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs)

The important lifetime idea:

- request-scoped state belongs in scoped services like `TenantContext`
- database work uses scoped contexts or shared infrastructure services as appropriate
- cache entries can live beyond a request, but must be invalidated after lifecycle and migration changes

## Review Checklist For Future Infrastructure Changes

- Does a new configurable schema value affect EF model caching?
- Does a new tenant state need registry validation?
- Does a shared entity have an explicit shared-schema mapping?
- Does a tenant migration accidentally include shared registry tables?
- Does onboarding still create `Tenant + first Clinic`?
- Does auth continue storing tenant IDs, not clinic IDs?
- Does any service bypass `TenancyOptions` with hardcoded `shared` or `tenant_`?
- Does a lifecycle or migration handler invalidate `TenantRegistry` cache?

## Short Lesson

Tenancy is where "small shortcuts" become launch blockers. A misplaced field, a
cached stale status, or an implicit schema mapping can turn into data isolation
risk. The current implementation is more explicit because explicit boundaries are
easier to review.
