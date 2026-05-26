# Research: Tenant Provisioning

**Feature**: 003-tenant-provisioning  
**Date**: 2026-05-23  
**Status**: Complete

---

## R1: Shared Schema Layout

**Decision**: Use an explicit PostgreSQL `shared` schema for cross-tenant domain data: `tenants`, `clinics`, `dentists`, `clinic_dentists`, tenant schema metadata, and provisioning audit logs. Keep ASP.NET Identity and refresh-token tables in `public`. Keep operational data in tenant schemas named `tenant_*`.

**Rationale**: `public` already hosts Identity/auth data from Phase 002. Cross-tenant domain data needs a stable location independent of whichever tenant schema is active. An explicit `shared` schema makes this boundary visible in migrations, EF mappings, and Dapper SQL.

**Alternatives considered**:
- **Keep shared data in `public`**: Works technically, but mixes auth tables with domain registry tables and makes ownership unclear.
- **Duplicate clinic/dentist rows per tenant schema**: Violates the dentist-sharing model and creates synchronization risk.

---

## R2: Schema Name Generation

**Decision**: Generate schema names deterministically from the tenant/practice ID using `tenant_` plus a lowercase, hyphen-free short ID segment, constrained to valid PostgreSQL identifier characters.

**Rationale**: Practice and clinic names are not unique and can contain spaces, punctuation, Arabic text, or characters that are awkward in PostgreSQL identifiers. Tenant ID-derived names are stable, unique, and safe. The `Tenant.SchemaName` property remains immutable after creation.

**Alternatives considered**:
- **Slug from clinic name**: User-friendly but collision-prone and rename-sensitive.
- **Random schema suffix**: Unique but not deterministic, making support and recovery harder.

---

## R3: Atomic Tenant Provisioning

**Decision**: Provision a tenant through a dedicated `ITenantProvisioningService` implementation that uses a dedicated PostgreSQL connection, a database transaction, and an advisory lock around schema creation and tenant migration. On any failure, roll back transactional work and explicitly drop the schema if it was created outside a completed transaction.

**Rationale**: PostgreSQL DDL is transactional, so `CREATE SCHEMA`, registry writes, and migration history writes can participate in a single transaction when executed on the same connection. An advisory lock prevents concurrent provisioning/migration work from overwhelming the database or racing on schema metadata.

**Alternatives considered**:
- **Create clinic record first, then provision schema later**: Simpler but violates the no-orphan requirement.
- **Background provisioning**: Better for long-running enterprise onboarding, but v1 success criteria require synchronous readiness.

---

## R4: Applying Tenant Migrations

**Decision**: Add a dedicated tenant migration service for operational tables. Tenant migrations target a runtime schema and maintain each tenant's `__EFMigrationsHistory` table inside that tenant schema. Existing tenants are upgraded by enumerating active registry rows and applying pending tenant migrations under a per-migration advisory lock.

**Rationale**: New tenants need the same operational model as existing tenants, and future releases must update every tenant schema. Keeping migration history per tenant lets the system know which schemas are current, partially migrated, or unhealthy.

**Alternatives considered**:
- **Use a single public migration history for all tenants**: Cannot represent per-tenant drift or partial migration failures.
- **Copy a template schema**: Fast, but PostgreSQL template cloning for live schema evolution becomes harder to reason about than migrations.

---

## R5: Runtime Schema Switching

**Decision**: Resolve tenant metadata into a request-scoped `ITenantContext`, then set PostgreSQL `search_path` whenever EF Core or Dapper opens a connection. The path is `"{tenant_schema}", shared, public` for tenant-scoped requests. Shared queries must still schema-qualify shared tables or use mappings to `shared`.

**Rationale**: Search path is connection session state. With pooled connections, it must be applied for each opened connection and must never rely on previous state. A scoped tenant context keeps Application code free of PostgreSQL details while Infrastructure applies the correct schema at the boundary.

**Alternatives considered**:
- **Global query filters by TenantId**: Useful for row-level tenancy, but this product requires schema-per-tenant isolation.
- **One connection string per tenant**: Safe but operationally heavy and unnecessary for schema-level isolation in one database.

---

## R6: Tenant Resolution and Status Checks

**Decision**: Tenant resolution reads `tenant_id` from validated JWT claims, fetches the tenant registry row from `shared.tenants`, verifies the tenant is active and the schema is healthy, then stores the resolved context in `HttpContext.Items`/`ITenantContext`. Missing or invalid tenant claims return 401. Known but inactive tenants return 403. Missing/unhealthy schemas return an explicit tenant provisioning error.

**Rationale**: JWT proves tenant membership, but the registry is the source of truth for current lifecycle state and schema health. This prevents deactivated or broken tenants from reaching handlers.

**Alternatives considered**:
- **Trust the JWT alone**: Fails deactivation and schema-health requirements until tokens expire.
- **Fallback to `tenant_dev` or `public`**: Dangerous data-leak risk; explicitly rejected.

---

## R7: Tenant Registry Caching

**Decision**: Cache tenant registry lookups by tenant ID for a short TTL, with cache invalidation on activation, deactivation, contact update, and migration health changes.

**Rationale**: Tenant resolution happens on every request, but status changes are relatively rare. Short-lived cache keeps request overhead low while still respecting lifecycle changes quickly.

**Alternatives considered**:
- **No cache**: Simplest and acceptable for early development, but unnecessary database pressure at 10-100 concurrent users per tenant.
- **Long-lived distributed cache**: Not needed until multi-instance scale or high tenant count.

---

## R8: Platform Operator Authorization

**Decision**: Protect tenant-management endpoints with a platform-level policy such as `Policies.CanManageTenants`. The policy must not reuse tenant-level `ClinicAdmin`. Its backing mechanism can be a platform role/claim or internal API-key handler, but the controller contract depends only on the policy.

**Rationale**: Tenant onboarding and lifecycle operations affect every clinic and must be isolated from clinic-scoped roles introduced in Phase 002.

**Alternatives considered**:
- **Reuse `ClinicAdmin`**: Incorrect privilege boundary.
- **Leave endpoints anonymous for internal tools**: Unacceptable for production and hard to test securely.
