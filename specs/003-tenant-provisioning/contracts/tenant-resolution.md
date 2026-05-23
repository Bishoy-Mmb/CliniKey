# Contract: Tenant Resolution

**Component**: `TenantResolutionMiddleware` plus `ITenantContext`/`ITenantRegistry`  
**Applies to**: All tenant-scoped API requests after authentication and before controllers/handlers.

---

## Request Input

Tenant-scoped requests require a valid JWT with:

```json
{
  "sub": "user-guid",
  "tenant_id": "clinic-guid",
  "role": "ClinicAdmin"
}
```

The middleware must not trust `X-Tenant-Id` in production. A debug-only override may exist for local integration tests, but must not be enabled in production configuration.

---

## Resolution Flow

1. Skip tenant resolution for anonymous auth endpoints and platform tenant-management endpoints.
2. Require an authenticated principal.
3. Read and parse `tenant_id` from JWT claims.
4. Query the tenant registry in `shared.clinics`/`shared.tenant_schema_health`.
5. Reject if the clinic does not exist.
6. Reject if the clinic status is not `Active`.
7. Reject if the schema health is not `Healthy`.
8. Store resolved values in `ITenantContext` and `HttpContext.Items`.
9. Continue to the next middleware/controller.

---

## Resolved Tenant Context

```json
{
  "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
  "schemaName": "tenant_3f0d1b0b",
  "clinicStatus": "Active",
  "schemaHealthStatus": "Healthy"
}
```

---

## Error Responses

### Missing or Invalid Tenant Claim

```http
401 Unauthorized
```

```json
{
  "title": "Tenant.InvalidClaim",
  "status": 401,
  "detail": "A valid tenant_id claim is required."
}
```

### Tenant Not Found

```http
401 Unauthorized
```

```json
{
  "title": "Tenant.NotFound",
  "status": 401,
  "detail": "The requested tenant could not be resolved."
}
```

### Tenant Inactive or Suspended

```http
403 Forbidden
```

```json
{
  "title": "Tenant.Inactive",
  "status": 403,
  "detail": "The clinic is inactive."
}
```

### Tenant Schema Unhealthy

```http
409 Conflict
```

```json
{
  "title": "Tenant.SchemaUnhealthy",
  "status": 409,
  "detail": "The clinic schema is not ready for use."
}
```

---

## Database Connection Contract

Tenant-scoped EF Core and Dapper connections must apply:

```sql
SET search_path TO "<tenant_schema>", shared, public;
```

Requirements:

- Quote schema identifiers safely; never concatenate untrusted raw input.
- Set the search path on every opened pooled connection.
- Do not proceed with an empty schema name.
- Shared-table queries should still be explicitly mapped/schema-qualified to `shared`.
- Integration tests must prove concurrent requests for different tenants do not share search path state.

