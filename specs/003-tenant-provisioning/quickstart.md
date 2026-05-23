# Quickstart: Tenant Provisioning

**Feature**: 003-tenant-provisioning  
**Date**: 2026-05-23

---

## Prerequisites

- .NET 10 SDK
- PostgreSQL 17 running locally
- Phase 001 domain model implemented
- Phase 002 Identity/Auth implemented
- Existing `AuthDbContext` migrations applied to `public`

---

## Configuration

Add tenant provisioning settings:

```json
{
  "Tenancy": {
    "SharedSchema": "shared",
    "TenantSchemaPrefix": "tenant_",
    "TenantRegistryCacheSeconds": 30,
    "ProvisioningLockKey": 3003,
    "RunTenantMigrationsOnStartup": false
  }
}
```

Development may enable startup tenant migration:

```json
{
  "Tenancy": {
    "RunTenantMigrationsOnStartup": true
  }
}
```

---

## Database Migrations

Generate shared-schema migrations for clinic registry and cross-tenant tables:

```bash
dotnet ef migrations add AddSharedTenantRegistry \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context SharedDbContext \
  --output-dir Persistence/Migrations/Shared
```

Generate tenant operational migrations separately:

```bash
dotnet ef migrations add InitialTenantOperationalSchema \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context AppDbContext \
  --output-dir Persistence/Migrations/Tenant
```

Apply shared migrations:

```bash
dotnet ef database update \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context SharedDbContext
```

Tenant migrations are applied by the provisioning service for new schemas and by the tenant migration command for existing schemas.

---

## Manual Flow

Start the API:

```bash
dotnet run --project src/CliniKey.API
```

Onboard a clinic:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/clinics \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -d '{
    "name": "Cairo Dental Center",
    "phone": "01112345678",
    "address": "15 Tahrir St, Cairo"
  }'
```

Verify the clinic registry:

```bash
curl http://localhost:5000/api/v1/tenants/clinics \
  -H "Authorization: Bearer {platformOperatorToken}"
```

Retrieve one clinic:

```bash
curl http://localhost:5000/api/v1/tenants/clinics/{clinicId} \
  -H "Authorization: Bearer {platformOperatorToken}"
```

Update clinic contact details:

```bash
curl -X PUT http://localhost:5000/api/v1/tenants/clinics/{clinicId}/contact \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "01198765432",
    "address": "22 Nile Corniche, Cairo"
  }'
```

Verify tenant schema isolation by logging in as a user whose JWT contains the new clinic ID and creating/querying tenant data:

```bash
curl http://localhost:5000/api/v1/patients \
  -H "Authorization: Bearer {tenantUserAccessToken}"
```

Deactivate a clinic:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/clinics/{clinicId}/deactivate \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Temporary closure"}'
```

Reactivate it:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/clinics/{clinicId}/activate \
  -H "Authorization: Bearer {platformOperatorToken}"
```

Apply pending tenant migrations:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/migrations/apply \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -H "Content-Type: application/json" \
  -d '{"includeInactive": true}'
```

Check tenant schema health:

```bash
curl http://localhost:5000/api/v1/tenants/migrations/status \
  -H "Authorization: Bearer {platformOperatorToken}"
```

---

## Key Integration Points

1. Map `Clinic`, `Dentist`, and `ClinicDentist` to the `shared` schema.
2. Add tenant registry/provisioning abstractions under `Application/Abstractions/Tenancy`.
3. Add `TenantResolutionMiddleware` logic that reads JWT `tenant_id`, verifies active healthy tenant registry state, and stores resolved schema in `ITenantContext`.
4. Ensure EF Core and Dapper set `search_path` for every opened tenant-scoped connection.
5. Add platform-operator authorization policy for tenant management endpoints.
6. Add integration tests with two provisioned tenant schemas and concurrent requests.
