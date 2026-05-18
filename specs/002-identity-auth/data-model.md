# Data Model: Identity & Authentication

**Feature**: 002-identity-auth  
**Date**: 2026-05-18  

---

## Entity: ApplicationUser

**Storage**: `public.asp_net_users` (ASP.NET Identity managed table)  
**Extends**: `IdentityUser<Guid>`

| Field | Type | Constraints | Notes |
|-------|------|------------|-------|
| `Id` | `Guid` | PK, auto-generated | Inherited from IdentityUser |
| `Email` | `string` | Required, unique, max 256 | Inherited from IdentityUser |
| `PasswordHash` | `string` | Required | Managed by Identity (PBKDF2) |
| `TenantId` | `Guid` | Required, FK → `public.clinics` | Links user to a clinic |
| `DentistId` | `Guid?` | Nullable, FK → tenant `dentists` | Only set for Dentist role users |
| `IsActive` | `bool` | Required, default `true` | Deactivation flag |
| `FullName` | `string` | Required, max 200 | Display name |
| `CreatedAtUtc` | `DateTime` | Required, auto-set | Audit timestamp |

**Identity-managed fields** (not listed above): `UserName`, `NormalizedEmail`, `NormalizedUserName`, `EmailConfirmed`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`.

**Relationships**:
- `ApplicationUser` → `Clinic`: Many-to-one (each user belongs to one clinic/tenant)
- `ApplicationUser` → `Dentist`: One-to-one optional (only Dentist role users)
- `ApplicationUser` → `RefreshToken`: One-to-many

---

## Entity: RefreshToken

**Storage**: `public.refresh_tokens`

| Field | Type | Constraints | Notes |
|-------|------|------------|-------|
| `Id` | `Guid` | PK, auto-generated | |
| `TokenHash` | `string` | Required, indexed | SHA256 hash of the raw token |
| `UserId` | `Guid` | Required, FK → `asp_net_users.Id` | Owner |
| `FamilyId` | `Guid` | Required, indexed | Groups tokens in a rotation chain |
| `ExpiresAtUtc` | `DateTime` | Required | 7-day TTL from creation |
| `CreatedAtUtc` | `DateTime` | Required | |
| `RevokedAtUtc` | `DateTime?` | Nullable | Set when revoked (used or security invalidation) |
| `ReplacedByTokenId` | `Guid?` | Nullable, FK → self | Points to the successor token |

**State transitions**:
```
Active (RevokedAtUtc == null, ExpiresAtUtc > now)
  → Used (RevokedAtUtc set, ReplacedByTokenId set → new Active token)
  → Expired (ExpiresAtUtc ≤ now, automatic — no DB write needed)
  → Revoked (RevokedAtUtc set, ReplacedByTokenId null → security invalidation)
```

**Replay detection**: If a token with `RevokedAtUtc != null` is presented, all tokens in the same `FamilyId` are revoked.

---

## ASP.NET Identity Tables (auto-managed)

These tables are created by EF Core Identity migrations in the `public` schema. They use Identity's default naming but are mapped to `snake_case` via Npgsql conventions:

| Table | Purpose |
|-------|---------|
| `asp_net_users` | User accounts (extended by `ApplicationUser`) |
| `asp_net_roles` | Role definitions (`ClinicAdmin`, `Dentist`, `Receptionist`) |
| `asp_net_user_roles` | User-role assignments |
| `asp_net_user_claims` | Additional user claims (not used in v1) |
| `asp_net_role_claims` | Role-level claims (not used in v1) |
| `asp_net_user_logins` | External login providers (not used in v1) |
| `asp_net_user_tokens` | Identity tokens (password reset, etc. — not used in v1) |

---

## Seeded Data

### Roles (seeded in `AuthDbContext.OnModelCreating`)

| Id | Name |
|----|------|
| `aaaaaaaa-0001-0001-0001-000000000001` | `ClinicAdmin` |
| `aaaaaaaa-0001-0001-0001-000000000002` | `Dentist` |
| `aaaaaaaa-0001-0001-0001-000000000003` | `Receptionist` |

### Dev Admin User (seeded for development only)

| Field | Value |
|-------|-------|
| Email | `admin@dev.clinikey.app` |
| Password | `Dev@2026!` |
| Role | `ClinicAdmin` |
| TenantId | `11111111-1111-1111-1111-111111111111` (existing Dev Clinic) |

---

## Schema Layout

```
PostgreSQL
├── public (shared schema)
│   ├── asp_net_users          ← NEW
│   ├── asp_net_roles          ← NEW
│   ├── asp_net_user_roles     ← NEW
│   ├── asp_net_user_claims    ← NEW (unused in v1)
│   ├── asp_net_role_claims    ← NEW (unused in v1)
│   ├── asp_net_user_logins    ← NEW (unused in v1)
│   ├── asp_net_user_tokens    ← NEW (unused in v1)
│   ├── refresh_tokens         ← NEW
│   ├── clinics                ← EXISTS
│   ├── dentists               ← EXISTS (cross-tenant reference)
│   └── clinic_dentists        ← EXISTS
├── tenant_dev (tenant schema)
│   ├── patients               ← EXISTS
│   ├── appointments           ← EXISTS
│   ├── treatment_plans        ← EXISTS
│   ├── treatment_items        ← EXISTS
│   ├── invoices               ← EXISTS
│   ├── invoice_lines          ← EXISTS
│   └── payments               ← EXISTS
└── tenant_XXX (other tenants)
    └── ... (same structure as tenant_dev)
```

---

## EF Core Migration Strategy

1. **AuthDbContext** gets its own migration history table: `__EFMigrationsHistory_Auth` in the `public` schema.
2. **AppDbContext** retains its existing migration history in each tenant schema.
3. Auth migrations run once (public schema). Tenant migrations run per-schema.
4. Migration command: `dotnet ef migrations add InitialAuth --context AuthDbContext --output-dir Infrastructure/Identity/Migrations`
