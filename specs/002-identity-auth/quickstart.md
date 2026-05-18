# Quickstart: Identity & Authentication

**Feature**: 002-identity-auth  
**Date**: 2026-05-18

---

## Prerequisites

- .NET 10 SDK
- PostgreSQL 16 running locally
- Existing CliniKey database with Phase 001 migrations applied

## New NuGet Packages

```xml
<!-- CliniKey.Infrastructure.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.*" />

<!-- CliniKey.API.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
```

## Configuration

Add to `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "YOUR-256-BIT-SECRET-KEY-MINIMUM-32-CHARS",
    "Issuer": "CliniKey",
    "Audience": "CliniKey.API",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

Add to `appsettings.Development.json`:

```json
{
  "Jwt": {
    "SecretKey": "CliniKey-Dev-Secret-Key-Minimum-32-Characters!"
  }
}
```

## Database Migrations

```bash
# Generate the auth migration
dotnet ef migrations add InitialAuth \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context AuthDbContext \
  --output-dir Identity/Migrations

# Apply
dotnet ef database update \
  --project src/CliniKey.Infrastructure \
  --startup-project src/CliniKey.API \
  --context AuthDbContext
```

## Testing the Flow

```bash
# 1. Register
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@dev.clinikey.app","password":"Dev@2026!","fullName":"Dev Admin","clinicId":"11111111-1111-1111-1111-111111111111"}'

# 2. Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@dev.clinikey.app","password":"Dev@2026!"}'

# 3. Access protected endpoint (use the accessToken from step 2)
curl http://localhost:5000/api/v1/patients \
  -H "Authorization: Bearer {accessToken}"

# 4. Invite a dentist
curl -X POST http://localhost:5000/api/v1/staff/invite \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {accessToken}" \
  -d '{"email":"dentist@dev.clinikey.app","password":"Dentist@2026!","fullName":"Dr. Dev","role":"Dentist","specialization":"General Dentistry","licenseNumber":"LIC-DEV-002"}'
```

## Key Integration Points

1. **Program.cs**: Add Identity, JWT Bearer auth, and authorization services
2. **TenantResolutionMiddleware**: Update to read `tenant_id` from JWT claims
3. **All controllers**: Add `[Authorize(Roles = "...")]` attributes
4. **DependencyInjection.cs**: Register `IAuthService`, `IJwtTokenService`, `ICurrentUserService`
