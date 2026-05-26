# Code Reading Guide: Identity & Authentication

Do not read this feature alphabetically. Read it by flow. Authentication code can
look scattered until you see the chain from HTTP request, to MediatR command, to
Identity service, to JWT claims, to tenant resolution.

## Mindset

Your job is to answer four questions:

1. How does a user account get created?
2. How does login turn database state into signed claims?
3. How do those claims protect tenant-scoped endpoints?
4. Where can security behavior silently drift from the spec?

Keep a notebook of claim names, route names, and policy names. In auth code, those
small strings are often where big bugs hide.

## Study Pass 1: Start With Intent

Read:

- [spec.md](../../specs/002-identity-auth/spec.md)
- [plan.md](../../specs/002-identity-auth/plan.md)
- [tasks.md](../../specs/002-identity-auth/tasks.md)
- [contracts/auth.md](../../specs/002-identity-auth/contracts/auth.md)
- [contracts/staff.md](../../specs/002-identity-auth/contracts/staff.md)

Questions to ask:

- Which endpoints are supposed to be anonymous?
- Which role can invite staff?
- Which JWT claims are required?
- What does the spec say should happen for duplicate emails, weak passwords, inactive users, and refresh replay?

What you are learning:

The intended behavior before implementation details enter the picture.

Mini exercise:

Make a two-column table called "spec says" and "code says." Leave the code column
empty for now. You will fill it in during later passes.

## Study Pass 2: Read The API Boundary

Read:

- [Program.cs](../../src/CliniKey.API/Program.cs)
- [AuthController.cs](../../src/CliniKey.API/Controllers/AuthController.cs)
- [PatientsController.cs](../../src/CliniKey.API/Controllers/PatientsController.cs)
- [AppointmentsController.cs](../../src/CliniKey.API/Controllers/AppointmentsController.cs)
- [TreatmentPlansController.cs](../../src/CliniKey.API/Controllers/TreatmentPlansController.cs)
- [InvoicesController.cs](../../src/CliniKey.API/Controllers/InvoicesController.cs)
- [Policies.cs](../../src/CliniKey.Application/Constants/Policies.cs)
- [Roles.cs](../../src/CliniKey.Application/Constants/Roles.cs)

Questions to ask:

- What runs first: authentication, tenant resolution, or authorization?
- Which endpoints use `[AllowAnonymous]`?
- Which endpoints use named policies?
- Are controller routes the same as the contracts?

What you are learning:

The HTTP surface and authorization vocabulary. This is where a secure use case
becomes reachable, blocked, or accidentally public.

Mini exercise:

Trace `POST /api/v1/auth/login`, `POST /api/v1/auth/invite`, and
`GET /api/v1/patients` through the middleware order in [Program.cs](../../src/CliniKey.API/Program.cs).
Write down which ones need a token and which ones need tenant resolution.

## Study Pass 3: Read The Application Contract

Read:

- [IAuthService.cs](../../src/CliniKey.Application/Abstractions/Identity/IAuthService.cs)
- [IJwtTokenService.cs](../../src/CliniKey.Application/Abstractions/Identity/IJwtTokenService.cs)
- [ICurrentUserService.cs](../../src/CliniKey.Application/Abstractions/Identity/ICurrentUserService.cs)
- [AuthErrors.cs](../../src/CliniKey.Application/Features/Auth/AuthErrors.cs)
- [RegisterCommandHandler.cs](../../src/CliniKey.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs)
- [LoginCommandHandler.cs](../../src/CliniKey.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs)
- [InviteStaffCommandHandler.cs](../../src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommandHandler.cs)
- [RefreshTokenCommandHandler.cs](../../src/CliniKey.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs)
- [GetCurrentUserQueryHandler.cs](../../src/CliniKey.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs)
- [GetUserByIdQueryHandler.cs](../../src/CliniKey.Application/Features/Auth/Queries/GetUserById/GetUserByIdQueryHandler.cs)

Questions to ask:

- Why does Application define `IAuthService` instead of referencing `UserManager<ApplicationUser>`?
- Which errors are validation, conflict, not-found, or failure?
- Are handlers doing business work or delegating orchestration?

What you are learning:

Clean Architecture dependency direction. Application knows what identity behavior
it needs; Infrastructure knows how Identity implements it.

Mini exercise:

Pick one handler and rewrite its behavior in one sentence. For example:
`LoginCommandHandler` turns `LoginCommand` into `IAuthService.LoginAsync` and
returns the resulting `Result<TokenResponse>`.

## Study Pass 4: Read Validation Before Behavior

Read:

- [RegisterCommandValidator.cs](../../src/CliniKey.Application/Features/Auth/Commands/Register/RegisterCommandValidator.cs)
- [LoginCommandValidator.cs](../../src/CliniKey.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs)
- [InviteStaffCommandValidator.cs](../../src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommandValidator.cs)
- [RefreshTokenCommandValidator.cs](../../src/CliniKey.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandValidator.cs)
- [ValidationExtensions.cs](../../src/CliniKey.Application/Extensions/ValidationExtensions.cs)
- [Dentist.cs](../../src/CliniKey.Domain/Entities/Dentist.cs)

Questions to ask:

- Which rules are request-shape rules?
- Which rules come from domain constants?
- Why does dentist invitation require specialization and license number?

What you are learning:

Validation is part of the use case contract. It blocks bad input before the
Infrastructure layer starts creating Identity users or domain records.

Mini exercise:

Write one invalid request for each command and predict whether the failure should
come from FluentValidation, Identity, Domain, or `AuthService`.

## Study Pass 5: Read Identity Storage

Read:

- [ApplicationUser.cs](../../src/CliniKey.Infrastructure/Identity/ApplicationUser.cs)
- [RefreshToken.cs](../../src/CliniKey.Infrastructure/Identity/RefreshToken.cs)
- [AuthDbContext.cs](../../src/CliniKey.Infrastructure/Identity/AuthDbContext.cs)
- [ApplicationUserConfiguration.cs](../../src/CliniKey.Infrastructure/Identity/Configurations/ApplicationUserConfiguration.cs)
- [RefreshTokenConfiguration.cs](../../src/CliniKey.Infrastructure/Identity/Configurations/RefreshTokenConfiguration.cs)
- [20260518190727_InitialAuth.cs](../../src/CliniKey.Infrastructure/Persistence/Migrations/Auth/20260518190727_InitialAuth.cs)
- [20260518192257_IdentityFixes.cs](../../src/CliniKey.Infrastructure/Persistence/Migrations/Auth/20260518192257_IdentityFixes.cs)
- [20260523000000_RemoveRolesSeedData.cs](../../src/CliniKey.Infrastructure/Persistence/Migrations/Auth/20260523000000_RemoveRolesSeedData.cs)

Questions to ask:

- Which fields are Identity defaults and which are CliniKey additions?
- Why does `AuthDbContext` set the default schema to `public`?
- Why store `TokenHash` instead of the raw refresh token?
- Why seed roles through [Program.cs](../../src/CliniKey.API/Program.cs) instead of migration data?

What you are learning:

The persistence model behind identity. This is also where auth and tenancy first
touch: `ApplicationUser.TenantId` is auth data that points at clinic context.

Mini exercise:

Draw the relationship between `ApplicationUser`, `RefreshToken`, `Clinic`, and
optional `Dentist`. Mark which records live in Identity/public storage and which
belong to domain/shared storage.

## Study Pass 6: Read Login And Token Creation

Read:

- [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs)
- [JwtTokenService.cs](../../src/CliniKey.Infrastructure/Identity/JwtTokenService.cs)
- [JwtSettings.cs](../../src/CliniKey.Infrastructure/Identity/JwtSettings.cs)
- [TokenResponse.cs](../../src/CliniKey.Application/DTOs/TokenResponse.cs)
- [JwtTokenServiceTests.cs](../../tests/CliniKey.Tests/Auth/JwtTokenServiceTests.cs)

Questions to ask:

- How does login avoid email enumeration?
- Where is the user's active state checked?
- Where is the clinic's active state checked?
- Which exact claim name is used for tenant id?
- How does `TimeProvider` make expiry testable?

What you are learning:

How persisted user state becomes signed request context.

Mini exercise:

From memory, list every claim emitted by [JwtTokenService.cs](../../src/CliniKey.Infrastructure/Identity/JwtTokenService.cs).
Then check your answer against [JwtTokenServiceTests.cs](../../tests/CliniKey.Tests/Auth/JwtTokenServiceTests.cs).

## Study Pass 7: Read Refresh Token Rotation

Read:

- [RefreshToken.cs](../../src/CliniKey.Infrastructure/Identity/RefreshToken.cs)
- [RefreshTokenCommandHandler.cs](../../src/CliniKey.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs)
- [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs)
- [RefreshTokenCommandHandlerTests.cs](../../tests/CliniKey.Tests/Auth/RefreshTokenCommandHandlerTests.cs)

Questions to ask:

- When is a refresh token active?
- What happens when a revoked token is presented again?
- How does the new token stay in the same family?
- What would happen if two refresh requests for the same token arrive together?

What you are learning:

Refresh tokens are not just longer JWTs. They are server-side security state.

Mini exercise:

Write the state transition for a refresh token from active to used. Include
`RevokedAtUtc`, `ReplacedByTokenId`, and `FamilyId`.

## Study Pass 8: Read Staff Invitation

Read:

- [InviteStaffCommand.cs](../../src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommand.cs)
- [InviteStaffCommandValidator.cs](../../src/CliniKey.Application/Features/Auth/Commands/InviteStaff/InviteStaffCommandValidator.cs)
- [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs)
- [Clinic.cs](../../src/CliniKey.Domain/Entities/Clinic.cs)
- [Dentist.cs](../../src/CliniKey.Domain/Entities/Dentist.cs)
- [DentistRepository.cs](../../src/CliniKey.Infrastructure/Persistence/Repositories/DentistRepository.cs)
- [CrossTenantDentistQueryTests.cs](../../tests/CliniKey.Tests/Infrastructure/CrossTenantDentistQueryTests.cs)

Questions to ask:

- Why does inviting a dentist create both a user and a dentist?
- Which tenant id is assigned to the invited user?
- What happens if Identity user creation succeeds but role assignment fails?
- Which save operation persists the dentist and clinic-dentist association?

What you are learning:

Some auth use cases cross into domain data. The code has to keep user identity
and clinical identity synchronized enough for the rest of the app.

Mini exercise:

Compare a receptionist invite with a dentist invite. List the extra records and
fields created only for dentists.

## Study Pass 9: Read Tenant Claim Resolution

Read:

- [TenantResolutionMiddleware.cs](../../src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs)
- [CurrentUserService.cs](../../src/CliniKey.Infrastructure/Identity/CurrentUserService.cs)
- [TenantResolutionMiddlewareTests.cs](../../tests/CliniKey.Tests/API/TenantResolutionMiddlewareTests.cs)
- [CurrentUserServiceTests.cs](../../tests/CliniKey.Tests/Auth/CurrentUserServiceTests.cs)
- [ITenantRegistry.cs](../../src/CliniKey.Application/Abstractions/Tenancy/ITenantRegistry.cs)
- [TenantContext.cs](../../src/CliniKey.Infrastructure/Persistence/TenantContext.cs)

Questions to ask:

- Which paths skip tenant resolution?
- Why is missing `tenant_id` a 401 instead of silently falling back?
- What gets written into `HttpContext.Items`?
- What does the tenant context setter receive?

What you are learning:

Authentication becomes tenancy only after the signed claim is resolved against
the tenant registry and stored in scoped tenant context.

Mini exercise:

Add `/api/v1/auth/register` to your "spec says/code says" table. Decide whether
the current behavior looks intentional, accidental, or unresolved.

## Important Terms

| Term | Meaning in this feature |
| --- | --- |
| `ApplicationUser` | ASP.NET Identity user extended with tenant, dentist, active, full-name, and created-at fields |
| `AuthDbContext` | EF Core Identity context mapped to the `public` schema |
| Access token | Short-lived signed JWT used on API requests |
| Refresh token | Long-lived random secret stored as a hash and rotated after use |
| Token family | Group of refresh tokens that belong to one login/rotation chain |
| `tenant_id` | Signed JWT claim that identifies the user's clinic context |
| `ClaimTypes.Role` | Role claim used by ASP.NET authorization |
| Policy | Named authorization rule such as `CanInviteStaff` |
| `ICurrentUserService` | Application-facing abstraction over the current claims principal |
| Replay | Reuse of an already-revoked refresh token |

## Common Junior Misreadings

| Misreading | Better reading |
| --- | --- |
| "The JWT proves the tenant exists forever." | The JWT carries a signed tenant id; middleware still resolves that tenant through the registry. |
| "Refresh tokens are just another token string." | Refresh tokens are server-side state and must be hashed, rotated, and revocable. |
| "The command handlers are where auth logic lives." | The handlers are mostly adapters; [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs) owns the orchestration. |
| "Roles and policies are interchangeable." | Roles are facts about users; policies are named access decisions at API boundaries. |
| "Register, login, and refresh all skip tenant resolution." | Current middleware skips login and refresh, plus tenants/openapi/scalar routes. Register needs a decision. |
| "Identity users are domain users." | In this codebase, Identity users are infrastructure auth records linked to domain concepts. |

## Red Flags For Future Work

- New auth endpoints without explicit `[Authorize]`, `[AllowAnonymous]`, or named policy decisions.
- New JWT claims added in [JwtTokenService.cs](../../src/CliniKey.Infrastructure/Identity/JwtTokenService.cs) without matching readers and tests.
- Any return of raw refresh token values from persistence.
- Password checks outside ASP.NET Identity.
- Tenant ids accepted from client input when the authenticated user context should decide.
- More role strings copied into controllers instead of using [Policies.cs](../../src/CliniKey.Application/Constants/Policies.cs).
- Tests that mock away the only security behavior they claim to prove.
- Time assertions using `DateTime.UtcNow` instead of `TimeProvider` or fixed test time.

## Final Study Challenge

Answer these without opening the code, then verify:

1. What exact claims does an access token contain?
2. Which middleware reads `tenant_id`?
3. Why is `AuthDbContext` separate from tenant-scoped app data?
4. What happens when a refresh token is reused after rotation?
5. Which policy allows staff invitation?
6. Why does dentist invitation need domain writes in addition to Identity writes?
7. Which endpoint-route detail differs between [contracts/staff.md](../../specs/002-identity-auth/contracts/staff.md) and [AuthController.cs](../../src/CliniKey.API/Controllers/AuthController.cs)?
8. What verification is still worth adding around [AuthService.cs](../../src/CliniKey.Infrastructure/Identity/AuthService.cs)?

When you can answer those cleanly, you understand the feature well enough to
review the next auth change with real judgment.
