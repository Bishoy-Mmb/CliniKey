# API Contracts: Authentication

**Feature**: 002-identity-auth  
**Base Path**: `/api/v1/auth`

---

## POST /api/v1/auth/register

Register a new ClinicAdmin account linked to an existing clinic.

**Authorization**: `[AllowAnonymous]`

**Request**:
```json
{
  "email": "admin@clinic.com",
  "password": "P@ssw0rd!",
  "fullName": "Ahmed Hassan",
  "clinicId": "11111111-1111-1111-1111-111111111111"
}
```

**Validation**:
- `email`: Required, valid email format, max 256 chars
- `password`: Required, min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit, 1 special char
- `fullName`: Required, max 200 chars
- `clinicId`: Required, must reference an existing active clinic

**Success Response** — `201 Created`:
```json
{
  "userId": "guid",
  "email": "admin@clinic.com",
  "role": "ClinicAdmin"
}
```

**Error Responses**:
| Status | Error Code | Condition |
|--------|-----------|-----------|
| 400 | `Validation.*` | Invalid input (email format, password complexity) |
| 409 | `Auth.DuplicateEmail` | Email already registered |
| 404 | `Clinic.NotFound` | ClinicId doesn't exist |

---

## POST /api/v1/auth/login

Authenticate a user and issue a JWT + refresh token pair.

**Authorization**: `[AllowAnonymous]`

**Request**:
```json
{
  "email": "admin@clinic.com",
  "password": "P@ssw0rd!"
}
```

**Success Response** — `200 OK`:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresAtUtc": "2026-05-18T16:00:00Z"
}
```

**Error Responses**:
| Status | Error Code | Condition |
|--------|-----------|-----------|
| 400 | `Validation.*` | Missing email or password |
| 401 | `Auth.InvalidCredentials` | Wrong email or password (generic — no enumeration) |
| 401 | `Auth.AccountDeactivated` | User exists but `IsActive == false` |

---

## POST /api/v1/auth/refresh

Exchange a valid refresh token for a new JWT + refresh token pair.

**Authorization**: `[AllowAnonymous]`

**Request**:
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Success Response** — `200 OK`:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
  "expiresAtUtc": "2026-05-18T17:00:00Z"
}
```

**Error Responses**:
| Status | Error Code | Condition |
|--------|-----------|-----------|
| 401 | `Auth.RefreshTokenExpired` | Token past expiry |
| 401 | `Auth.RefreshTokenRevoked` | Token already used or family-revoked (replay detection) |
| 401 | `Auth.InvalidRefreshToken` | Token not found in DB |

---

## GET /api/v1/auth/me

Get the current authenticated user's profile.

**Authorization**: `[Authorize]`

**Success Response** — `200 OK`:
```json
{
  "userId": "guid",
  "email": "admin@clinic.com",
  "fullName": "Ahmed Hassan",
  "role": "ClinicAdmin",
  "tenantId": "guid",
  "dentistId": null
}
```

**Error Responses**:
| Status | Error Code | Condition |
|--------|-----------|-----------|
| 401 | — | No or invalid JWT |
