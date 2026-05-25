# Implementation Plan: Tenant Provisioning

**Branch**: `003-tenant-provisioning` | **Date**: 2026-05-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/003-tenant-provisioning/spec.md`

## Summary

Implement the tenant provisioning lifecycle for CliniKey. This phase turns clinic onboarding into an atomic operation that creates a shared tenant/practice registry record, generates an immutable PostgreSQL tenant schema, creates the first clinic branch under that tenant, applies operational migrations to the tenant schema, and makes tenant-aware requests use the resolved tenant schema. It also moves cross-tenant data (`Tenant`, `Clinic`, `Dentist`, `ClinicDentist`) into an explicit `shared` schema, keeps auth data in `public`, and adds tenant activation/deactivation plus schema health checks.

## Technical Context

**Language/Version**: C# 14 / .NET 10  
**Primary Dependencies**: EF Core (Npgsql), Dapper, MediatR, FluentValidation, ASP.NET Core authorization, Npgsql advisory locks  
**Storage**: PostgreSQL 17 with three schema classes: `public` for Identity/auth, `shared` for tenant registry/cross-tenant data, `tenant_*` for operational clinic data  
**Testing**: xUnit, FluentAssertions, NSubstitute, Testcontainers, Microsoft.Extensions.Time.Testing  
**Target Platform**: Linux server (Docker container) / Windows development  
**Project Type**: Web API (REST)  
**Performance Goals**: Onboard a clinic in < 10s; tenant resolution < 50ms p95 with cache; request schema setup < 5ms overhead  
**Constraints**: Atomic provisioning, immutable schema names, no fallback/default tenant, tenant schema switching must be safe with pooled connections, no data loss on deactivation  
**Scale/Scope**: 10-100 concurrent users per tenant, 10-50 tenants in v1, concurrent requests across at least 10 tenants

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution file is present after Spec Kit refresh, but it contains the default placeholder constitution. Proceeding with the established project conventions from phases 001/002 and the CliniKey architecture standards.

| Gate | Status | Evidence |
|------|--------|----------|
| Clean Architecture dependency flow | Pass | Domain models tenant lifecycle and clinic branches; Application defines provisioning abstractions/features; Infrastructure owns PostgreSQL schema/migration implementation; API stays thin |
| CQRS via MediatR | Pass | Tenant lifecycle commands and registry queries are planned as Application vertical slices |
| Result<T> expected failures | Pass | Duplicate phone, inactive tenant, missing schema, unhealthy schema, and provisioning failures are explicit errors |
| Domain independence | Pass | Domain has no EF, Npgsql, ASP.NET, or migration references |
| Tenant isolation | Pass | Operational tables are tenant-schema scoped; shared cross-tenant data is explicitly schema-qualified |
| TimeProvider usage | Pass | Tenant and clinic lifecycle timestamps and tests use injected TimeProvider/FakeTimeProvider |
| Authorization standards | Pass | Tenant-management endpoints use a platform-operator policy; clinic users remain tenant-scoped |

## Project Structure

### Documentation (this feature)

```text
specs/003-tenant-provisioning/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- clinics.md
|   |-- tenant-resolution.md
|   `-- tenant-migrations.md
`-- tasks.md              # Phase 2 output via /speckit.tasks
```

### Source Code (repository root)

```text
src/
|-- CliniKey.Domain/
|   |-- Entities/
|   |   |-- Tenant.cs                 # practice isolation boundary, schema/provisioning/health
|   |   |-- Clinic.cs                 # branch/location under a tenant
|   |   `-- TenantProvisioningAuditLog.cs
|   |-- Enums/
|   |   |-- ClinicStatus.cs
|   |   |-- TenantProvisioningStatus.cs
|   |   `-- TenantSchemaHealthStatus.cs
|   |-- Events/
|   |   |-- TenantProvisionedEvent.cs
|   |   |-- TenantActivatedEvent.cs
|   |   `-- TenantDeactivatedEvent.cs
|   `-- Errors/
|       `-- TenantErrors.cs
|-- CliniKey.Application/
|   |-- Abstractions/
|   |   |-- Tenancy/
|   |   |   |-- ITenantContext.cs
|   |   |   |-- ITenantRegistry.cs
|   |   |   |-- ITenantProvisioningService.cs
|   |   |   `-- ITenantMigrationService.cs
|   |   `-- Data/
|   |       `-- IDbConnectionFactory.cs # tenant-aware overload or scoped schema support
|   `-- Features/
|       `-- Tenants/
|           |-- Commands/
|           |   |-- OnboardClinic/       # V1 creates Tenant + first Clinic
|           |   |-- ActivateClinic/
|           |   |-- DeactivateClinic/
|           |   |-- UpdateClinicContact/
|           |   `-- MigrateTenantSchemas/
|           `-- Queries/
|               |-- GetClinicById/
|               |-- ListClinics/
|               `-- GetTenantSchemaHealth/
|-- CliniKey.Infrastructure/
|   |-- Persistence/
|   |   |-- AppDbContext.cs            # operational tenant schema + shared mappings
|   |   |-- SharedDbContext.cs         # optional focused context for shared registry data
|   |   |-- TenantContext.cs
|   |   |-- TenantConnectionInterceptor.cs
|   |   |-- TenantRegistry.cs
|   |   |-- TenantProvisioningService.cs
|   |   |-- TenantMigrationService.cs
|   |   |-- Configurations/
|   |   |   |-- TenantConfiguration.cs   # maps to shared.tenants
|   |   |   |-- ClinicConfiguration.cs   # maps to shared.clinics
|   |   |   |-- DentistConfiguration.cs  # maps to shared.dentists
|   |   |   `-- TenantProvisioningAuditLogConfiguration.cs
|   |   `-- Migrations/
|   |       |-- Shared/                 # shared schema migrations
|   |       `-- Tenant/                 # tenant operational schema migrations
|   `-- DependencyInjection.cs
|-- CliniKey.API/
|   |-- Controllers/
|   |   `-- TenantsController.cs
|   |-- Middleware/
|   |   `-- TenantResolutionMiddleware.cs
|   `-- Program.cs
`-- tests/
    `-- CliniKey.Tests/
        |-- Domain/
        |   `-- ClinicTests.cs
        |-- Application/
        |   `-- TenantProvisioningCommandHandlerTests.cs
        `-- Infrastructure/
            |-- TenantProvisioningIntegrationTests.cs
            |-- TenantSchemaSwitchingTests.cs
            `-- TenantMigrationServiceTests.cs
```

**Structure Decision**: Tenant lifecycle use cases live under `Application/Features/Tenants`. `Tenant`/practice is the isolation boundary and owns schema name, provisioning state, schema health, and current migration. `Clinic` is a branch/location under a tenant; V1 onboarding still accepts one clinic payload but creates both a tenant/practice and its first clinic internally. Infrastructure owns PostgreSQL-specific schema creation, migration, health checks, and connection search-path setup. Shared registry data can either use a focused `SharedDbContext` or explicit schema mappings in `AppDbContext`; implementation should choose the least disruptive option while keeping tenant operational mappings separate from shared mappings.

## Phase 0 Research Output

Completed in [research.md](./research.md). All technical unknowns are resolved.

## Phase 1 Design Output

Completed artifacts:

- [data-model.md](./data-model.md)
- [quickstart.md](./quickstart.md)
- [contracts/clinics.md](./contracts/clinics.md)
- [contracts/tenant-resolution.md](./contracts/tenant-resolution.md)
- [contracts/tenant-migrations.md](./contracts/tenant-migrations.md)

## Post-Design Constitution Check

| Gate | Status | Evidence |
|------|--------|----------|
| Clean Architecture dependency flow | Pass | Contracts place interfaces in Application and PostgreSQL behavior in Infrastructure |
| CQRS via MediatR | Pass | Commands/queries are documented for onboarding, lifecycle, listing, and migrations |
| Result<T> expected failures | Pass | Contracts and data model define validation/conflict/forbidden/error states |
| Domain independence | Pass | Tenant schema and migration mechanics are Application abstractions/Infrastructure implementations |
| Tenant isolation | Pass | Tenant request flow requires resolved active healthy tenant before DB work |
| TimeProvider usage | Pass | Lifecycle and audit timestamps are modelled as UTC fields set through injected time |
| Authorization standards | Pass | Platform-operator policy protects tenant-management endpoints; tenant middleware bypass is limited to auth and platform tenant routes |

## Complexity Tracking

| Decision | Why | Alternative Rejected |
|----------|-----|----------------------|
| Explicit `shared` schema for Tenant/Clinic/Dentist/ClinicDentist | Cross-tenant registry and branch/staff data must remain readable regardless of active tenant schema | Leaving these tables in whatever schema search path selects risks accidental tenant-local copies |
| Tenant connection interceptor + tenant-aware Dapper connection setup | Search path must be set every time a pooled connection is opened | Setting search path once at startup is unsafe because connections are pooled and reused |
| Dedicated tenant migration service | New schemas and existing schemas must be brought to the current operational model | Manually duplicating SQL scripts for each tenant is fragile and untestable |
| Transactional provisioning with rollback/drop-schema compensation | Caller must never see orphaned clinic records or orphaned schemas | Creating clinic then provisioning schema in separate independent steps leaves partial state |
| Platform operator policy for tenant management | Clinic roles are tenant-scoped and cannot safely manage all tenants | Reusing `ClinicAdmin` would let a clinic admin administer other tenants |
