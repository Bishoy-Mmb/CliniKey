# Contract: Tenant Schema Migrations

**Base route**: `/api/v1/tenants/migrations`  
**Authorization**: `Policies.CanManageTenants` platform-operator policy  
**Purpose**: Apply operational schema migrations to existing tenant schemas and report drift/health.

---

## POST `/api/v1/tenants/migrations/apply`

Apply pending tenant operational migrations to all matching tenant schemas.

### Request

```json
{
  "includeInactive": true,
  "tenantIds": [
    "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f"
  ]
}
```

`tenantIds` is optional. When omitted, all tenant schemas are considered. Inactive tenants are skipped unless `includeInactive` is true.

### Response `200 OK`

```json
{
  "startedAtUtc": "2026-05-23T11:00:00Z",
  "finishedAtUtc": "2026-05-23T11:00:04Z",
  "expectedMigration": "202605230001_InitialTenantOperationalSchema",
  "results": [
    {
      "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
      "schemaName": "tenant_3f0d1b0b",
      "status": "Succeeded",
      "previousMigration": "202605180001_InitialDomainSchema",
      "currentMigration": "202605230001_InitialTenantOperationalSchema",
      "message": null
    }
  ]
}
```

### Errors

| Status | Error code | Meaning |
|--------|------------|---------|
| 409 | `Tenant.MigrationAlreadyRunning` | Another tenant migration run holds the advisory lock |
| 500 | `Tenant.MigrationFailed` | One or more schemas failed; response includes failed results when possible |

---

## GET `/api/v1/tenants/migrations/status`

Return tenant schema migration and health summary.

### Response `200 OK`

```json
{
  "expectedMigration": "202605230001_InitialTenantOperationalSchema",
  "items": [
    {
      "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
      "schemaName": "tenant_3f0d1b0b",
      "tenantStatus": "Active",
      "schemaHealthStatus": "Healthy",
      "currentMigration": "202605230001_InitialTenantOperationalSchema",
      "lastSchemaVerifiedAtUtc": "2026-05-23T11:00:04Z",
      "failureReason": null
    }
  ]
}
```

---

## Startup Migration Behavior

When `Tenancy:RunTenantMigrationsOnStartup` is true:

1. Apply shared-schema migrations.
2. Resolve all tenant registry rows.
3. Apply pending tenant migrations under the advisory lock.
4. Mark failed schemas as `Unhealthy`.
5. Do not accept tenant-scoped traffic for schemas marked `Unhealthy`.

Production may run migrations through the explicit endpoint/command instead of startup.
