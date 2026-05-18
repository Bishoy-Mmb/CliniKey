# Implementation Plan: Identity & Authentication

**Branch**: `002-identity-auth` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/002-identity-auth/spec.md`

## Summary

Implement JWT-based authentication and role-based authorization for CliniKey. This phase introduces ASP.NET Identity for user management, JWT Bearer token issuance with tenant-aware claims, refresh token rotation, role-based endpoint security (`ClinicAdmin`, `Dentist`, `Receptionist`), and replaces the existing stub `X-Tenant-Id` header middleware with real JWT claim-based tenant resolution. All user/auth data lives in the PostgreSQL `public` schema; tenant-scoped operational data remains schema-isolated.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Identity, Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt  
**Storage**: PostgreSQL 16 (user/auth data in `public` schema via existing `AppDbContext` or a dedicated `AuthDbContext`)  
**Testing**: xUnit, FluentAssertions, NSubstitute, Testcontainers  
**Target Platform**: Linux server (Docker container)  
**Project Type**: Web API (REST)  
**Performance Goals**: Login < 500ms p95, JWT validation < 5ms overhead per request  
**Constraints**: Stateless authentication (no server-side sessions), short-lived access tokens (60 min), refresh tokens (7 days)  
**Scale/Scope**: 10–100 concurrent users per tenant, 10–50 tenants in v1

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution is a placeholder template — no project-specific gates defined. Proceeding with established project conventions from Phase 001:

| Gate | Status | Evidence |
|------|--------|----------|
| Clean Architecture (inward dependency flow) | ✅ Pass | Auth abstractions in Application layer, implementations in Infrastructure |
| CQRS via MediatR | ✅ Pass | Auth commands/queries follow existing pattern |
| Result<T> pattern for error handling | ✅ Pass | All auth handlers return Result<T> |
| Domain independence from infrastructure | ✅ Pass | No EF/ASP.NET references in Domain layer |
| Schema-per-tenant isolation | ✅ Pass | User data in public schema; tenant data in tenant schemas via existing mechanism |

## Project Structure

### Documentation (this feature)

```text
specs/002-identity-auth/
├── plan.md              # This file
├── research.md          # Phase 0 output — technology decisions
├── data-model.md        # Phase 1 output — entity/table design
├── quickstart.md        # Phase 1 output — integration guide
├── contracts/           # Phase 1 output — API endpoint contracts
│   ├── auth.md          # Login, register, refresh endpoints
│   └── staff.md         # Staff invitation endpoints
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── CliniKey.SharedKernel/           # No changes expected
├── CliniKey.Domain/
│   ├── Entities/                    # No changes to existing aggregates
│   └── Enums/
│       └── StaffRole.cs             # Already exists (Admin, Dentist, Receptionist)
├── CliniKey.Application/
│   ├── Abstractions/
│   │   └── Identity/
│   │       ├── IAuthService.cs          # Login, register, refresh, invite abstractions
│   │       ├── ICurrentUserService.cs   # Extracts user/tenant from ClaimsPrincipal
│   │       └── IJwtTokenService.cs      # Token generation/validation abstraction
│   └── Features/
│       └── Auth/
│           ├── Commands/
│           │   ├── Login/               # LoginCommand + Handler + Validator
│           │   ├── Register/            # RegisterCommand + Handler + Validator
│           │   ├── RefreshToken/        # RefreshTokenCommand + Handler
│           │   └── InviteStaff/         # InviteStaffCommand + Handler + Validator
│           └── Queries/
│               └── GetCurrentUser/      # GetCurrentUserQuery + Handler
├── CliniKey.Infrastructure/
│   ├── Identity/
│   │   ├── ApplicationUser.cs           # IdentityUser subclass with TenantId, DentistId
│   │   ├── AuthDbContext.cs             # IdentityDbContext for public schema
│   │   ├── AuthService.cs              # IAuthService implementation
│   │   ├── JwtTokenService.cs          # IJwtTokenService implementation
│   │   ├── CurrentUserService.cs       # ICurrentUserService implementation
│   │   ├── RefreshToken.cs             # Refresh token entity
│   │   └── Configurations/
│   │       ├── ApplicationUserConfiguration.cs
│   │       └── RefreshTokenConfiguration.cs
│   └── DependencyInjection.cs          # Updated: register auth services
├── CliniKey.API/
│   ├── Controllers/
│   │   └── AuthController.cs            # Login, register, refresh, invite endpoints
│   ├── Middleware/
│   │   ├── TenantResolutionMiddleware.cs  # UPDATED: resolve from JWT claims
│   │   └── GlobalExceptionMiddleware.cs   # No changes
│   └── Program.cs                       # UPDATED: add auth middleware, JWT config
└── tests/
    └── CliniKey.Tests/
        ├── Auth/
        │   ├── LoginCommandHandlerTests.cs
        │   ├── RegisterCommandHandlerTests.cs
        │   ├── InviteStaffCommandHandlerTests.cs
        │   └── JwtTokenServiceTests.cs
        └── Integration/
            └── AuthIntegrationTests.cs
```

**Structure Decision**: Auth abstractions (`IAuthService`, `IJwtTokenService`, `ICurrentUserService`) live in `Application/Abstractions/Identity/`. Implementations live in `Infrastructure/Identity/`. This follows the existing Clean Architecture pattern where Domain/Application define interfaces and Infrastructure implements them. A separate `AuthDbContext` (extending `IdentityDbContext`) is used for user management in the public schema, keeping it isolated from the tenant-scoped `AppDbContext`.

## Complexity Tracking

| Decision | Why | Alternative Rejected |
|----------|-----|---------------------|
| Separate `AuthDbContext` from `AppDbContext` | User data lives in public schema; tenant data lives in tenant schemas. Mixing them in one context risks schema switching affecting auth queries | Single DbContext — rejected because `SET search_path` for tenant isolation would break Identity queries |
| `ApplicationUser` extends `IdentityUser` | Need to add `TenantId`, `DentistId`, `IsActive` without fighting Identity's schema | Custom user table — rejected because it would require reimplementing password hashing, lockout, and role management |
| Refresh tokens stored in DB (not in-memory) | Must survive server restarts. Supports token family rotation for replay attack detection | In-memory cache — rejected because stateless architecture requires persistence |
