# Tasks: Identity & Authentication

**Input**: Design documents from `specs/002-identity-auth/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Tests included — following existing project convention from Phase 001 (unit tests for domain/application logic).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install NuGet packages, create project scaffolding, and configure JWT/Identity services.

- [ ] T001 Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package to `src/CliniKey.Infrastructure/CliniKey.Infrastructure.csproj`
- [ ] T002 Add `Microsoft.AspNetCore.Authentication.JwtBearer` package to `src/CliniKey.API/CliniKey.API.csproj`
- [ ] T003 Add JWT configuration section to `src/CliniKey.API/appsettings.json` and `src/CliniKey.API/appsettings.Development.json` with `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenExpirationMinutes`, `Jwt:RefreshTokenExpirationDays`
- [ ] T004 Create `JwtSettings.cs` options class in `src/CliniKey.Infrastructure/Identity/JwtSettings.cs` with properties matching the config section

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core identity infrastructure that MUST be complete before ANY user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 Create `ApplicationUser : IdentityUser<Guid>` in `src/CliniKey.Infrastructure/Identity/ApplicationUser.cs` with properties: `TenantId` (Guid), `DentistId` (Guid?), `IsActive` (bool), `FullName` (string), `CreatedAtUtc` (DateTime)
- [ ] T006 Create `RefreshToken` entity in `src/CliniKey.Infrastructure/Identity/RefreshToken.cs` with properties: `Id`, `TokenHash`, `UserId`, `FamilyId`, `ExpiresAtUtc`, `CreatedAtUtc`, `RevokedAtUtc`, `ReplacedByTokenId`
- [ ] T007 Create `AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` in `src/CliniKey.Infrastructure/Identity/AuthDbContext.cs` targeting the `public` schema, with `DbSet<RefreshToken>`, role seed data for `ClinicAdmin`, `Dentist`, `Receptionist`
- [ ] T008 Create `ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>` in `src/CliniKey.Infrastructure/Identity/Configurations/ApplicationUserConfiguration.cs` — map `TenantId`, `DentistId`, `IsActive`, `FullName`, `CreatedAtUtc` to snake_case columns
- [ ] T009 Create `RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>` in `src/CliniKey.Infrastructure/Identity/Configurations/RefreshTokenConfiguration.cs` — index on `TokenHash`, `FamilyId`, FK to `ApplicationUser`
- [ ] T010 Create `IAuthService` interface in `src/CliniKey.Application/Abstractions/Identity/IAuthService.cs` with methods: `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `InviteStaffAsync`
- [ ] T011 [P] Create `IJwtTokenService` interface in `src/CliniKey.Application/Abstractions/Identity/IJwtTokenService.cs` with methods: `GenerateAccessToken(ApplicationUser, string role)`, `GenerateRefreshToken()`
- [ ] T012 [P] Create `ICurrentUserService` interface in `src/CliniKey.Application/Abstractions/Identity/ICurrentUserService.cs` with properties: `UserId`, `Email`, `TenantId`, `Role`, `DentistId`, `IsAuthenticated`
- [ ] T013 Create `AuthErrors` static class in `src/CliniKey.Application/Features/Auth/AuthErrors.cs` defining: `InvalidCredentials`, `DuplicateEmail`, `AccountDeactivated`, `InvalidRefreshToken`, `RefreshTokenExpired`, `RefreshTokenRevoked`, `InvalidRole`, `WeakPassword`
- [ ] T014 Register ASP.NET Identity services, `AuthDbContext`, JWT Bearer authentication, and authorization in `src/CliniKey.API/Program.cs` — configure `PasswordOptions`, `TokenValidationParameters`, `AddAuthentication().AddJwtBearer()`
- [ ] T015 Register `IAuthService`, `IJwtTokenService`, `ICurrentUserService` in `src/CliniKey.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Foundation ready — all identity infrastructure is in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Register a Clinic Admin Account (Priority: P1) 🎯 MVP

**Goal**: A platform operator creates an admin account linked to a clinic, stores it in the public schema, and receives confirmation.

**Independent Test**: Call `POST /api/v1/auth/register` with valid data, verify 201 response with user ID and role `ClinicAdmin`.

### Implementation for User Story 1

- [ ] T016 [US1] Create `RegisterCommand` record in `src/CliniKey.Application/Features/Auth/Commands/Register/RegisterCommand.cs` with: `Email`, `Password`, `FullName`, `ClinicId`
- [ ] T017 [US1] Create `RegisterCommandValidator` in `src/CliniKey.Application/Features/Auth/Commands/Register/RegisterCommandValidator.cs` — validate email format, password complexity, FullName non-empty/max-200, ClinicId non-empty
- [ ] T018 [US1] Create `RegisterCommandHandler` in `src/CliniKey.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs` — use `UserManager<ApplicationUser>` to create user, assign `ClinicAdmin` role, return `Result<Guid>`. Check clinic exists, check email uniqueness
- [ ] T019 [US1] Create `AuthResponse` DTO in `src/CliniKey.Application/DTOs/AuthResponse.cs` with: `UserId`, `Email`, `Role`

### Tests for User Story 1

- [ ] T020 [P] [US1] Create `RegisterCommandHandlerTests` in `tests/CliniKey.Tests/Auth/RegisterCommandHandlerTests.cs` — test: successful registration, duplicate email returns `Auth.DuplicateEmail`, invalid clinic returns `Clinic.NotFound`

**Checkpoint**: Admin registration works independently. A user can be created in the DB.

---

## Phase 4: User Story 2 — Log In and Receive a JWT (Priority: P1)

**Goal**: A registered user logs in with email/password and receives a JWT + refresh token pair.

**Independent Test**: Register a user (US1), then call `POST /api/v1/auth/login`, verify JWT claims contain correct `sub`, `email`, `role`, `tenant_id`.

### Implementation for User Story 2

- [ ] T021 [US2] Create `JwtTokenService : IJwtTokenService` in `src/CliniKey.Infrastructure/Identity/JwtTokenService.cs` — generate JWT with claims (`sub`, `email`, `role`, `tenant_id`, `dentist_id`), generate refresh token (random bytes → Base64 → SHA256 hash for storage)
- [ ] T022 [US2] Create `LoginCommand` record in `src/CliniKey.Application/Features/Auth/Commands/Login/LoginCommand.cs` with: `Email`, `Password`
- [ ] T023 [US2] Create `LoginCommandValidator` in `src/CliniKey.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs` — validate email non-empty, password non-empty
- [ ] T024 [US2] Create `LoginCommandHandler` in `src/CliniKey.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs` — validate credentials via `UserManager.CheckPasswordAsync`, check `IsActive`, generate JWT + refresh token, store refresh token in DB, return `Result<TokenResponse>`
- [ ] T025 [US2] Create `TokenResponse` DTO in `src/CliniKey.Application/DTOs/TokenResponse.cs` with: `AccessToken`, `RefreshToken`, `ExpiresAtUtc`

### Tests for User Story 2

- [ ] T026 [P] [US2] Create `LoginCommandHandlerTests` in `tests/CliniKey.Tests/Auth/LoginCommandHandlerTests.cs` — test: successful login returns JWT, wrong password returns `Auth.InvalidCredentials`, deactivated user returns `Auth.AccountDeactivated`
- [ ] T027 [P] [US2] Create `JwtTokenServiceTests` in `tests/CliniKey.Tests/Auth/JwtTokenServiceTests.cs` — test: token contains correct claims, token expiry matches config, different users get different tokens

**Checkpoint**: Full register → login flow works. Users receive valid JWTs.

---

## Phase 5: User Story 3 — Secure Existing Endpoints with Role-Based Access (Priority: P1)

**Goal**: All existing API endpoints require JWT authentication. Each enforces role-based access control. Tenant isolation is enforced via JWT claims.

**Independent Test**: Call `GET /api/v1/patients` without JWT → 401. Call with Receptionist JWT → 200. Call with Dentist JWT on `POST /api/v1/treatmentplans` → 200. Call with Receptionist JWT on `POST /api/v1/treatmentplans` → 403.

### Implementation for User Story 3

- [ ] T028 [US3] Create `CurrentUserService : ICurrentUserService` in `src/CliniKey.Infrastructure/Identity/CurrentUserService.cs` — extract claims from `IHttpContextAccessor.HttpContext.User`
- [ ] T029 [US3] Update `TenantResolutionMiddleware` in `src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs` — resolve `tenant_id` from JWT `ClaimsPrincipal` instead of `X-Tenant-Id` header. Keep header fallback under `#if DEBUG` only
- [ ] T030 [US3] Add `[Authorize(Roles = "ClinicAdmin,Dentist,Receptionist")]` to `PatientsController` in `src/CliniKey.API/Controllers/PatientsController.cs`
- [ ] T031 [P] [US3] Add `[Authorize(Roles = "ClinicAdmin,Dentist,Receptionist")]` to `AppointmentsController` in `src/CliniKey.API/Controllers/AppointmentsController.cs`
- [ ] T032 [P] [US3] Add `[Authorize(Roles = "ClinicAdmin,Dentist")]` to `TreatmentPlansController` in `src/CliniKey.API/Controllers/TreatmentPlansController.cs`
- [ ] T033 [P] [US3] Add `[Authorize(Roles = "ClinicAdmin,Receptionist")]` to `InvoicesController` in `src/CliniKey.API/Controllers/InvoicesController.cs`
- [ ] T034 [US3] Create `AuthController` in `src/CliniKey.API/Controllers/AuthController.cs` with `[AllowAnonymous]` on `Register` and `Login` endpoints, `[Authorize]` on `GetCurrentUser` endpoint. Wire to MediatR commands

### Tests for User Story 3

- [ ] T035 [P] [US3] Create `CurrentUserServiceTests` in `tests/CliniKey.Tests/Auth/CurrentUserServiceTests.cs` — test: extracts correct claims, returns null/empty for unauthenticated context

**Checkpoint**: All existing endpoints are secured. Unauthenticated requests return 401. Unauthorized role requests return 403.

---

## Phase 6: User Story 4 — Invite Staff Members (Priority: P2)

**Goal**: A ClinicAdmin invites dentists and receptionists. Dentist invitations create both a user account and a `Dentist` entity.

**Independent Test**: Admin logs in, calls `POST /api/v1/staff/invite` with role `Dentist`, verify user + `Dentist` entity created. Invited dentist logs in and receives JWT with `dentist_id` claim.

### Implementation for User Story 4

- [ ] T036 [US4] Create `InviteStaffCommand` record in `src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommand.cs` with: `Email`, `Password`, `FullName`, `Role`, `Specialization`, `LicenseNumber`
- [ ] T037 [US4] Create `InviteStaffCommandValidator` in `src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommandValidator.cs` — validate email, password, role must be `Dentist` or `Receptionist`, specialization/license required when role is `Dentist`
- [ ] T038 [US4] Create `InviteStaffCommandHandler` in `src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommandHandler.cs` — create user via `UserManager`, assign role. If Dentist: create `Dentist` entity via `IDentistRepository`, create `ClinicDentist` link, set `ApplicationUser.DentistId`. Use `ICurrentUserService` for admin's `TenantId`
- [ ] T039 [US4] Add `POST /api/v1/staff/invite` endpoint to `AuthController` in `src/CliniKey.API/Controllers/AuthController.cs` with `[Authorize(Roles = "ClinicAdmin")]`

### Tests for User Story 4

- [ ] T040 [P] [US4] Create `InviteStaffCommandHandlerTests` in `tests/CliniKey.Tests/Auth/InviteStaffCommandHandlerTests.cs` — test: successful dentist invite creates user + Dentist entity, receptionist invite creates user only, non-admin caller returns 403, duplicate email returns `Auth.DuplicateEmail`

**Checkpoint**: Complete staff management flow. Admin can invite dentists/receptionists, and they can log in with correct roles.

---

## Phase 7: User Story 5 — Refresh Token (Priority: P3)

**Goal**: Users can exchange a valid refresh token for a new JWT without re-entering credentials. Replay detection revokes token families.

**Independent Test**: Login (get JWT + refresh token), call `POST /api/v1/auth/refresh` with the refresh token, verify new JWT + new refresh token returned. Use old refresh token again, verify rejection and family revocation.

### Implementation for User Story 5

- [ ] T041 [US5] Create `RefreshTokenCommand` record in `src/CliniKey.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs` with: `RefreshToken` (string)
- [ ] T042 [US5] Create `RefreshTokenCommandHandler` in `src/CliniKey.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs` — hash incoming token, look up in DB, validate not expired/revoked, detect replay (if revoked → revoke entire family), issue new JWT + new refresh token, revoke old token
- [ ] T043 [US5] Add `POST /api/v1/auth/refresh` endpoint to `AuthController` in `src/CliniKey.API/Controllers/AuthController.cs` with `[AllowAnonymous]`

### Tests for User Story 5

- [ ] T044 [P] [US5] Create `RefreshTokenCommandHandlerTests` in `tests/CliniKey.Tests/Auth/RefreshTokenCommandHandlerTests.cs` — test: successful refresh returns new tokens, expired token returns `Auth.RefreshTokenExpired`, reused token triggers family revocation

**Checkpoint**: Full auth lifecycle complete — register, login, secure endpoints, invite staff, refresh tokens.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, cleanup, and validation.

- [ ] T045 Create `GetCurrentUserQuery` and `GetCurrentUserQueryHandler` in `src/CliniKey.Application/Features/Auth/Queries/GetCurrentUser/` — return authenticated user's profile via `ICurrentUserService`
- [ ] T046 Add `GET /api/v1/auth/me` endpoint to `AuthController` with `[Authorize]`
- [ ] T047 Generate EF Core migration for `AuthDbContext` — run `dotnet ef migrations add InitialAuth --context AuthDbContext`
- [ ] T048 Verify `dotnet build CliniKey.slnx` compiles with 0 warnings, 0 errors
- [ ] T049 Verify `dotnet test` — all new and existing tests pass
- [ ] T050 Git commit all work on `002-identity-auth` branch

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) — BLOCKS all user stories
- **US1 Register (Phase 3)**: Depends on Phase 2
- **US2 Login (Phase 4)**: Depends on Phase 2 (can parallelize with US1 but logically depends on user existing)
- **US3 Secure Endpoints (Phase 5)**: Depends on Phase 2 + US2 (needs JWT middleware configured)
- **US4 Invite Staff (Phase 6)**: Depends on US3 (needs `[Authorize]` and `ICurrentUserService`)
- **US5 Refresh Token (Phase 7)**: Depends on US2 (needs login to generate initial refresh token)
- **Polish (Phase 8)**: Depends on all user stories

### User Story Dependencies

```
Phase 1 (Setup)
  └─→ Phase 2 (Foundational)
        ├─→ US1 (Register) ──→ US2 (Login) ──→ US3 (Secure) ──→ US4 (Invite)
        │                        └──→ US5 (Refresh)
        └─→ Phase 8 (Polish) — after all US complete
```

### Within Each User Story

- Models/commands before handlers
- Handlers before controllers
- Tests can be written in parallel with implementation (same phase)

### Parallel Opportunities

- T011, T012: `IJwtTokenService` and `ICurrentUserService` interfaces (different files)
- T030, T031, T032, T033: Controller authorize attributes (different files)
- T020, T026, T027, T035, T040, T044: All test files (independent)

---

## Implementation Strategy

### MVP First (User Stories 1–3)

1. Complete Phase 1: Setup (T001–T004)
2. Complete Phase 2: Foundational (T005–T015)
3. Complete US1: Register (T016–T020)
4. Complete US2: Login (T021–T027)
5. Complete US3: Secure Endpoints (T028–T035)
6. **STOP and VALIDATE**: Register → Login → Access protected endpoint workflow
7. Deploy/demo if ready — core auth works

### Full Delivery

8. Complete US4: Invite Staff (T036–T040)
9. Complete US5: Refresh Tokens (T041–T044)
10. Complete Polish (T045–T050)
11. Write ESR and commit

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All handlers follow existing MediatR CQRS pattern from Phase 001
- All handlers return `Result<T>` — no exceptions for business logic
- `AuthDbContext` migrations are separate from `AppDbContext` migrations
- User data lives in `public` schema — never in tenant schemas
- Commit after each phase or logical group
