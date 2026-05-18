# Research: Identity & Authentication

**Feature**: 002-identity-auth  
**Date**: 2026-05-18  
**Status**: Complete

---

## R1: ASP.NET Identity vs Custom User Management

**Decision**: Use ASP.NET Identity with a custom `ApplicationUser` extending `IdentityUser`.

**Rationale**: ASP.NET Identity provides battle-tested password hashing (PBKDF2 with 600K iterations in .NET 10), account lockout, role management, and claims-based identity — all out of the box. Building custom user management would require reimplementing security-critical code (password hashing, timing-safe comparison, brute-force protection) with no benefit.

**Alternatives considered**:
- **Custom user table + BCrypt**: Full control but requires manually implementing lockout, role management, claims transformation, and password policy enforcement. Unacceptable security risk for a SaaS product.
- **OpenIddict / IdentityServer**: Full OAuth2/OIDC server. Overkill for v1 where only email/password is needed. Can be adopted later if OAuth2 flows are required.

---

## R2: JWT Configuration

**Decision**: Short-lived access tokens (60 min) + long-lived refresh tokens (7 days) with single-use rotation.

**Configuration**:
- **Signing algorithm**: HMAC-SHA256 (symmetric). Adequate for a single-API deployment.
- **Issuer**: `CliniKey`
- **Audience**: `CliniKey.API`
- **Clock skew**: 0 (default 5 min is too generous — rely on exact expiry)

**Claims shape**:
```json
{
  "sub": "user-guid",
  "email": "user@clinic.com",
  "role": "Dentist",
  "tenant_id": "clinic-guid",
  "dentist_id": "dentist-guid",
  "iat": 1716048000,
  "exp": 1716051600
}
```

**Rationale**: 60-minute access tokens balance security (limited exposure window if token is leaked) with usability (user doesn't need to refresh constantly). 7-day refresh tokens match typical clinic operating patterns (staff work 5–6 days, don't want to re-login daily).

**Alternatives considered**:
- **RSA-SHA256 (asymmetric)**: Required if multiple services need to validate tokens independently. Overkill for a monolithic API. Can be migrated to RS256 if microservices are introduced.
- **Sliding expiration**: Extends token lifetime on each request. Rejected because it defeats the purpose of short-lived tokens — a compromised token would never expire if actively used.

---

## R3: Separate AuthDbContext vs Shared AppDbContext

**Decision**: Separate `AuthDbContext` extending `IdentityDbContext<ApplicationUser>`, targeting the `public` schema.

**Rationale**: The existing `AppDbContext` dynamically switches `search_path` for tenant isolation. If Identity tables were in `AppDbContext`, a login request would need to resolve the tenant *before* authentication — a circular dependency (you need to authenticate to know the tenant, but you need the tenant to query the right schema). A separate `AuthDbContext` hardcoded to the `public` schema breaks this cycle.

**Alternatives considered**:
- **Shared AppDbContext with schema override**: Would require conditional schema switching logic — "use public schema for Identity tables, tenant schema for everything else." Complex, error-prone, and violates single-responsibility.
- **Separate database entirely**: Maximum isolation but adds operational complexity (two connection strings, two migration targets). Overkill for the current scale.

---

## R4: Refresh Token Storage Strategy

**Decision**: Database-backed refresh tokens with family rotation.

**Implementation**:
- `RefreshToken` entity: `Id`, `Token` (SHA256 hash), `UserId`, `ExpiresAtUtc`, `CreatedAtUtc`, `RevokedAtUtc`, `ReplacedByTokenId`, `FamilyId`.
- On refresh: old token is marked revoked, new token is created with same `FamilyId`.
- On replay (already-revoked token is reused): all tokens in the family are revoked (security measure).
- Expired/revoked tokens are cleaned up by a periodic background task.

**Rationale**: Database storage survives server restarts and supports multi-instance deployments. Family rotation detects token theft — if an attacker uses a stolen refresh token after the legitimate user has already refreshed, the family revocation locks out the attacker (and the user, who must re-authenticate, but is alerted to the compromise).

**Alternatives considered**:
- **In-memory cache (IMemoryCache)**: Lost on restart, doesn't work with multiple instances. Rejected.
- **Redis**: Adds an infrastructure dependency not present in the stack. Unnecessary for < 100 concurrent users. Can be adopted later.
- **JWT-based refresh tokens (no DB)**: Cannot be revoked server-side. Rejected for security reasons.

---

## R5: Role-Based Access Control Implementation

**Decision**: ASP.NET Identity roles + `[Authorize(Roles = "...")]` on controllers + policy-based authorization for fine-grained rules.

**Role mapping**:

| StaffRole (Domain) | ASP.NET Identity Role | Permissions |
|--------------------|-----------------------|-------------|
| `Admin` | `ClinicAdmin` | Full CRUD within tenant |
| `Dentist` | `Dentist` | TreatmentPlans (full), Appointments (own), Patients (read) |
| `Receptionist` | `Receptionist` | Patients (full), Appointments (full), Invoices (full) |

**Controller-level authorization**:
```
PatientsController:      [Authorize(Roles = "ClinicAdmin,Dentist,Receptionist")]
AppointmentsController:  [Authorize(Roles = "ClinicAdmin,Dentist,Receptionist")]
TreatmentPlansController:[Authorize(Roles = "ClinicAdmin,Dentist")]
InvoicesController:      [Authorize(Roles = "ClinicAdmin,Receptionist")]
AuthController:          Mixed — Register/Login are [AllowAnonymous], Invite is [Authorize(Roles = "ClinicAdmin")]
```

**Alternatives considered**:
- **Claims-based authorization only**: More granular but harder to audit. Roles provide clear, understandable access boundaries for a clinic context.
- **Permission-based (custom)**: Would require a permissions table and middleware. Overkill for 3 roles with well-defined boundaries.

---

## R6: Tenant Resolution Migration

**Decision**: Replace `X-Tenant-Id` header with JWT `tenant_id` claim. Keep header for development/testing only (behind `#if DEBUG`).

**Flow**:
1. Request arrives with `Authorization: Bearer {jwt}` header.
2. ASP.NET JWT middleware validates the token and populates `HttpContext.User`.
3. Updated `TenantResolutionMiddleware` extracts `tenant_id` from `ClaimsPrincipal`.
4. Middleware looks up the tenant's `SchemaName` from the tenant registry (cached).
5. `AppDbContext` uses the resolved schema for all subsequent queries.

**Rationale**: The current `X-Tenant-Id` header is a security hole — any client can impersonate any tenant. JWT claims are cryptographically signed and cannot be tampered with.

**Alternatives considered**:
- **Subdomain-based tenant resolution**: Requires DNS wildcard setup and SSL cert management. More complex for Egyptian hosting infrastructure. Can be added later as an additional resolution strategy.

---

## R7: Password Policy

**Decision**: Minimum 8 characters, at least one uppercase, one lowercase, one digit, one special character.

**Implementation**: Configured via ASP.NET Identity's `PasswordOptions` in `Program.cs`.

**Rationale**: Balances security with usability for Egyptian clinic staff who may not be technically sophisticated. Weaker policies invite brute-force attacks; stronger policies (16+ chars) lead to password reuse or sticky notes on monitors.
