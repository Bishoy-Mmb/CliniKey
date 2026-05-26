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

Apply auth migrations first so Identity roles and users can be stored in `public`:

```bash
dotnet ef database update \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context AuthDbContext
```

Generate shared-schema migrations for the tenant registry, clinic branches, and cross-tenant tables:

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

## Development Platform Operator

In Development, API startup creates a local platform operator if it does not already exist:

```text
email: operator@clinikey.local
password: CliniKeyDev#12345
```

The user is assigned to the seeded development tenant:

```text
11111111-1111-1111-1111-111111111111
```

Override the defaults with configuration or environment variables:

```json
{
  "DevelopmentSeed": {
    "PlatformOperator": {
      "Email": "operator@clinikey.local",
      "Password": "CliniKeyDev#12345",
      "FullName": "Development Platform Operator"
    }
  }
}
```

After the API starts, log in at `POST /api/v1/auth/login` and use the returned
`accessToken` as the platform operator token.

---

## Manual Flow

Start the API:

```bash
dotnet run --project src/CliniKey.API
```

Log in as the development platform operator:

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "operator@clinikey.local",
    "password": "CliniKeyDev#12345"
  }'
```

Onboard a clinic:

```bash
curl -X POST http://localhost:5000/api/v1/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -d '{
    "name": "Cairo Dental Center",
    "phone": "01112345678",
    "address": "15 Tahrir St, Cairo"
  }'
```

Verify the tenant registry:

```bash
curl http://localhost:5000/api/v1/tenants \
  -H "Authorization: Bearer {platformOperatorToken}"
```

Retrieve one tenant:

```bash
curl http://localhost:5000/api/v1/tenants/{tenantId} \
  -H "Authorization: Bearer {platformOperatorToken}"
```

Update clinic contact details:

```bash
curl -X PUT http://localhost:5000/api/v1/tenants/{tenantId}/clinics/{clinicId}/contact \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -H "Content-Type: application/json" \
  -d '{
    "phone": "01198765432",
    "address": "22 Nile Corniche, Cairo"
  }'
```

Verify tenant schema isolation by logging in as a user whose JWT contains the new tenant ID and creating/querying tenant data:

```bash
curl http://localhost:5000/api/v1/patients \
  -H "Authorization: Bearer {tenantUserAccessToken}"
```

Deactivate a clinic:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/{tenantId}/deactivate \
  -H "Authorization: Bearer {platformOperatorToken}" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Temporary closure"}'
```

Reactivate it:

```bash
curl -X POST http://localhost:5000/api/v1/tenants/{tenantId}/activate \
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
