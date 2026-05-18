# Feature Specification: Identity & Authentication

**Feature Branch**: `002-identity-auth`  
**Created**: 2026-05-18  
**Status**: Draft  
**Input**: User description: "Implement JWT-based authentication and role-based authorization for the CliniKey Dental SaaS. This phase secures all existing API endpoints, introduces user account management, and replaces the stub X-Tenant-Id header-based tenant resolution with JWT claim-based resolution. The system must support the existing role hierarchy (Admin, Dentist, Receptionist) and correctly scope all operations to the authenticated user's tenant."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register a Clinic Admin Account (Priority: P1)

A platform operator (or the first user of a new clinic) creates an admin account for the clinic. The system provisions a user record with the `ClinicAdmin` role, associates it with a specific clinic (tenant), and returns credentials that can be used to log in.

**Why this priority**: Without a registered user, no one can authenticate. This is the entry point for all subsequent operations.

**Independent Test**: Can be fully tested by calling the registration endpoint with valid data, then verifying the user exists and has the correct role and tenant association. Delivers immediate value as the gateway to all other features.

**Acceptance Scenarios**:

1. **Given** a clinic with ID `clinic-001` exists, **When** an admin registers with email `admin@clinic.com` and password `P@ssw0rd!`, **Then** the system creates a user with role `ClinicAdmin`, associates it with `clinic-001`, and returns a success response with the user ID.
2. **Given** a user with email `admin@clinic.com` already exists, **When** another registration attempt uses the same email, **Then** the system returns an `Auth.DuplicateEmail` error.
3. **Given** the password `123` is submitted, **When** registration is attempted, **Then** the system returns a validation error because the password does not meet complexity requirements (minimum 8 characters, at least one uppercase, one lowercase, one digit, one special character).

---

### User Story 2 - Log In and Receive a JWT (Priority: P1)

A registered user (admin, dentist, or receptionist) enters their email and password. The system validates credentials and returns a JWT containing the user's identity, role, and tenant information. The token is used for all subsequent API calls.

**Why this priority**: Authentication is the prerequisite for authorization. Co-equal with US1 — together they form the minimum viable identity system.

**Independent Test**: Can be tested by registering a user (US1), then logging in and verifying the returned JWT contains the correct claims (`sub`, `email`, `role`, `tenant_id`).

**Acceptance Scenarios**:

1. **Given** user `admin@clinic.com` exists with password `P@ssw0rd!`, **When** they log in with correct credentials, **Then** the system returns a JWT with claims `sub` (user ID), `email`, `role` (`ClinicAdmin`), `tenant_id` (the clinic's ID), and an expiry of 60 minutes.
2. **Given** user `admin@clinic.com` exists, **When** they log in with an incorrect password, **Then** the system returns an `Auth.InvalidCredentials` error. The response does not distinguish between wrong email and wrong password (to prevent email enumeration).
3. **Given** a valid JWT has expired, **When** the user attempts to access a protected endpoint, **Then** the system returns 401 Unauthorized.
4. **Given** a user submits a JWT with a tampered signature, **When** the API validates the token, **Then** the system returns 401 Unauthorized.

---

### User Story 3 - Secure Existing Endpoints with Role-Based Access (Priority: P1)

All existing API endpoints (Patients, Appointments, TreatmentPlans, Invoices) are secured behind JWT authentication. Each endpoint enforces role-based access control: ClinicAdmins can do everything within their tenant, Dentists can manage treatments and view appointments, Receptionists can manage patients, appointments, and invoices.

**Why this priority**: Securing endpoints is the entire reason this phase exists. Without it, the API is publicly accessible.

**Independent Test**: Can be tested by calling a protected endpoint without a token (expecting 401), with a token of insufficient role (expecting 403), and with a valid token (expecting success).

**Acceptance Scenarios**:

1. **Given** no JWT is provided, **When** a user calls `GET /api/v1/patients`, **Then** the system returns 401 Unauthorized.
2. **Given** a Receptionist JWT is provided, **When** they call `POST /api/v1/patients`, **Then** the operation succeeds (Receptionists can create patients).
3. **Given** a Receptionist JWT is provided, **When** they call `POST /api/v1/treatmentplans`, **Then** the system returns 403 Forbidden (only Dentists and Admins can create treatment plans).
4. **Given** a Dentist from Clinic A has a valid JWT, **When** they call `GET /api/v1/patients` scoped to Clinic B, **Then** the system returns 403 Forbidden (tenant isolation enforcement).
5. **Given** a ClinicAdmin JWT is provided, **When** they call any endpoint within their tenant, **Then** the operation succeeds (Admins have full access within their tenant).

---

### User Story 4 - Invite Staff Members (Priority: P2)

A ClinicAdmin invites a dentist or receptionist to the clinic. The system creates a user account with the specified role, linked to the admin's tenant. The invited user can then log in with the credentials provided.

**Why this priority**: Clinics need multiple staff members to operate. Depends on US1-3 being complete.

**Independent Test**: Can be tested by a ClinicAdmin inviting a dentist, then the dentist logging in and verifying they can access clinical endpoints.

**Acceptance Scenarios**:

1. **Given** an authenticated ClinicAdmin, **When** they invite `dentist@clinic.com` with role `Dentist` and specialization `Orthodontics`, **Then** the system creates a user account, links it to the clinic's tenant, and creates a `Dentist` entity associated with the user.
2. **Given** an authenticated Receptionist, **When** they attempt to invite a new user, **Then** the system returns 403 Forbidden (only ClinicAdmins can invite staff).
3. **Given** a ClinicAdmin invites a user with role `Dentist`, **When** the invited user logs in, **Then** their JWT contains `role: Dentist` and `dentist_id: {guid}`.
4. **Given** a ClinicAdmin invites a user with email `existing@clinic.com` that already exists in the system, **When** the invitation is processed, **Then** the system returns an `Auth.DuplicateEmail` error.

---

### User Story 5 - Refresh Token (Priority: P3)

An authenticated user whose JWT is about to expire can request a new token without re-entering credentials. The system validates the refresh token and issues a new JWT.

**Why this priority**: Improves user experience by avoiding frequent re-authentication, but the system works without it (users can re-login).

**Independent Test**: Can be tested by logging in (getting a JWT + refresh token), waiting for JWT expiry, then using the refresh token to get a new JWT.

**Acceptance Scenarios**:

1. **Given** a user logs in successfully, **When** they receive their JWT, **Then** they also receive a refresh token with a 7-day expiry.
2. **Given** a valid refresh token, **When** the user calls `POST /api/v1/auth/refresh`, **Then** the system issues a new JWT and a new refresh token (rotation).
3. **Given** an expired refresh token, **When** the user attempts to refresh, **Then** the system returns `Auth.RefreshTokenExpired` and the user must re-login.
4. **Given** a refresh token has been used once, **When** it is used again (replay attack), **Then** the system rejects it and invalidates all tokens for that user (family rotation).

---

### Edge Cases

- What happens when a user's account is deactivated while they hold a valid JWT? → The JWT remains valid until expiry. On next request, a middleware check against the user's `IsActive` flag returns 401 if deactivated. Short-lived tokens (60 min) limit the exposure window.
- What happens when a ClinicAdmin changes a user's role? → The user's next token refresh issues a JWT with the updated role. The current JWT remains valid with the old role until expiry.
- How does the system handle a dentist working at multiple clinics? → In v1, a dentist has one user account per clinic. Cross-clinic identity unification is deferred to Phase 013 (SaaS Subscription). This matches the current `ClinicDentist` join model.
- What if the JWT signing key is rotated? → All existing JWTs become invalid. Users must re-authenticate. Key rotation is an operational procedure, not an in-app feature.
- What if a user forgets their password? → Password reset is out of scope for v1. ClinicAdmin can deactivate and re-invite with new credentials.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST authenticate users via email/password and issue a signed JWT upon successful login.
- **FR-002**: System MUST include in the JWT: `sub` (user ID), `email`, `role`, `tenant_id`, and optionally `dentist_id` (if the user is a dentist).
- **FR-003**: System MUST enforce JWT validation on all existing API endpoints. Unauthenticated requests MUST receive 401 Unauthorized.
- **FR-004**: System MUST enforce role-based access control. Users accessing resources beyond their role MUST receive 403 Forbidden.
- **FR-005**: System MUST enforce tenant isolation. A user's JWT MUST only grant access to data within their tenant's schema. Cross-tenant access MUST return 403 Forbidden.
- **FR-006**: System MUST support user registration for ClinicAdmin accounts, linked to an existing clinic.
- **FR-007**: System MUST support ClinicAdmin-initiated staff invitation for Dentist and Receptionist roles.
- **FR-008**: System MUST enforce password complexity: minimum 8 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character.
- **FR-009**: System MUST issue refresh tokens alongside JWTs. Refresh tokens MUST support single-use rotation.
- **FR-010**: System MUST replace the existing `X-Tenant-Id` header-based tenant resolution with JWT `tenant_id` claim resolution.
- **FR-011**: System MUST store user accounts in a shared (non-tenant-scoped) schema, since users are cross-tenant entities.
- **FR-012**: System MUST prevent email enumeration in error responses (login failures return a generic `Auth.InvalidCredentials` error).
- **FR-013**: System MUST support user deactivation by ClinicAdmin. Deactivated users MUST be denied access on subsequent requests.
- **FR-014**: When inviting a Dentist, the system MUST create both a user account and a `Dentist` entity, linking them via the user ID or a shared identifier.

### Key Entities

- **User**: The authentication identity. Wraps ASP.NET Identity's `IdentityUser`. Key attributes: email, password hash, role, tenant ID, is active, associated dentist ID (if applicable). Stored in the shared/public schema.
- **RefreshToken**: A single-use token for obtaining new JWTs without re-authentication. Key attributes: token value (hashed), user ID, expiry date, is revoked. Stored in the shared/public schema.
- **Role**: Maps to the existing `StaffRole` enum (Admin, Dentist, Receptionist). Implemented via ASP.NET Identity roles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can register, log in, and access a protected endpoint within a single workflow in under 5 seconds total (excluding network latency).
- **SC-002**: 100% of existing API endpoints return 401 when called without a valid JWT.
- **SC-003**: A user with Receptionist role receives 403 when attempting to create a treatment plan (role enforcement).
- **SC-004**: A user from Tenant A receives 403 when attempting to access Tenant B's data (tenant isolation).
- **SC-005**: JWT validation correctly rejects expired tokens, tampered tokens, and tokens signed with the wrong key.
- **SC-006**: Refresh token rotation correctly invalidates used tokens and issues new pairs.
- **SC-007**: The system does not leak password details, token internals, or tenant information in error responses.

## Assumptions

- ASP.NET Identity is used as the user management framework. It provides battle-tested password hashing, lockout policies, and role management without custom implementation.
- Password reset (forgot password) is out of scope for v1. ClinicAdmin handles credential recovery by deactivating and re-inviting.
- Email verification is out of scope for v1. Emails are assumed valid at registration.
- Two-factor authentication (2FA) is out of scope for v1.
- OAuth2/social login is out of scope for v1. Only email/password authentication is supported.
- The existing `Clinic` and `Dentist` entities remain unchanged. The new `User` entity references them via foreign keys.
- Cross-clinic dentist identity (a single dentist account across multiple clinics) is deferred. In v1, each clinic creates its own user account for shared dentists.
- All user data (accounts, refresh tokens) is stored in the PostgreSQL `public` schema, not in tenant-scoped schemas.

## Review & Acceptance Checklist

- [x] All user stories have clear acceptance scenarios with Given/When/Then
- [x] Priorities are assigned and justified
- [x] Each story is independently testable
- [x] Edge cases are identified and have defined behavior
- [x] Key entities and their relationships are documented
- [x] Role hierarchy is explicitly defined
- [x] Tenant isolation implications are addressed
- [x] Success criteria are measurable and technology-agnostic
- [x] Assumptions clearly scope what is and isn't included
- [x] Security considerations (enumeration, token tampering, replay attacks) are addressed
