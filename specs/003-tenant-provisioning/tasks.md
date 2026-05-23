# Tasks: Tenant Provisioning

**Input**: Design documents from `specs/003-tenant-provisioning/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/

**Tests**: Included because the specification defines independent tests and measurable integration criteria for each user story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label for story phases only
- All tasks include exact repository file paths

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add tenant provisioning configuration and platform authorization constants.

- [X] T001 Add `Tenancy` configuration section to `src/CliniKey.API/appsettings.json` and `src/CliniKey.API/appsettings.Development.json`
- [X] T002 Add `PlatformOperator` role constant to `src/CliniKey.Application/Constants/Roles.cs`
- [X] T003 Add `CanManageTenants` policy constant to `src/CliniKey.Application/Constants/Policies.cs`
- [X] T004 Register `Policies.CanManageTenants` authorization policy and seed the `PlatformOperator` role in `src/CliniKey.API/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core tenant domain, shared schema mapping, and tenancy abstractions required before user stories.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T005 [P] Create `ClinicStatus` enum in `src/CliniKey.Domain/Enums/ClinicStatus.cs`
- [X] T006 [P] Create `TenantProvisioningStatus` enum in `src/CliniKey.Domain/Enums/TenantProvisioningStatus.cs`
- [X] T007 [P] Create `TenantSchemaHealthStatus` enum in `src/CliniKey.Domain/Enums/TenantSchemaHealthStatus.cs`
- [X] T008 Extend `Clinic` with phone, address, lifecycle status, provisioning status, schema health, migration, deactivation, and schema verification fields in `src/CliniKey.Domain/Entities/Clinic.cs`
- [X] T009 Add clinic contact, lifecycle, provisioning, and schema health errors to `src/CliniKey.Domain/Errors/ClinicErrors.cs`
- [X] T010 [P] Create `TenantErrors` in `src/CliniKey.Domain/Errors/TenantErrors.cs`
- [X] T011 [P] Create clinic lifecycle domain events in `src/CliniKey.Domain/Events/ClinicProvisionedEvent.cs`, `src/CliniKey.Domain/Events/ClinicActivatedEvent.cs`, `src/CliniKey.Domain/Events/ClinicDeactivatedEvent.cs`, and `src/CliniKey.Domain/Events/ClinicContactUpdatedEvent.cs`
- [X] T012 [P] Create `TenantProvisioningAuditLog` entity in `src/CliniKey.Domain/Entities/TenantProvisioningAuditLog.cs`
- [X] T013 Extend `IClinicRepository` with add, list, phone-exists, schema lookup, and status update methods in `src/CliniKey.Domain/Repositories/IClinicRepository.cs`
- [X] T014 [P] Create `TenancyOptions` in `src/CliniKey.Infrastructure/Persistence/TenancyOptions.cs`
- [X] T015 [P] Create tenancy abstractions in `src/CliniKey.Application/Abstractions/Tenancy/ITenantContext.cs`, `ITenantRegistry.cs`, `ITenantProvisioningService.cs`, and `ITenantMigrationService.cs`
- [X] T016 Create `SharedDbContext` for shared schema registry tables in `src/CliniKey.Infrastructure/Persistence/SharedDbContext.cs`
- [X] T017 Update `ClinicConfiguration`, `DentistConfiguration`, and `ClinicDentistConfiguration` to map to the `shared` schema in `src/CliniKey.Infrastructure/Persistence/Configurations/`
- [X] T018 [P] Create `TenantProvisioningAuditLogConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/TenantProvisioningAuditLogConfiguration.cs`
- [X] T019 Register `TenancyOptions`, `SharedDbContext`, tenancy services, and tenant-aware connection services in `src/CliniKey.Infrastructure/DependencyInjection.cs`
- [X] T020 Update `AppDbContext` seed data for Dev Clinic contact/status/provisioning fields in `src/CliniKey.Infrastructure/Persistence/AppDbContext.cs`

**Checkpoint**: Domain model, shared schema mappings, and tenancy service contracts are ready.

---

## Phase 3: User Story 1 - Onboard a New Clinic (Priority: P1) MVP

**Goal**: A platform operator creates a clinic, provisions a tenant schema, applies operational migrations, and receives the clinic ID.

**Independent Test**: Call `POST /api/v1/tenants/clinics`, verify `shared.clinics` has the clinic and the new tenant schema contains operational tables.

### Tests for User Story 1

- [X] T021 [P] [US1] Add clinic creation/contact validation tests in `tests/CliniKey.Tests/Domain/ClinicTests.cs`
- [X] T022 [P] [US1] Add onboarding handler tests for success, duplicate phone, and provisioning failure rollback in `tests/CliniKey.Tests/Application/OnboardClinicCommandHandlerTests.cs`
- [X] T023 [P] [US1] Add provisioning integration tests for schema creation, migration application, and no orphan state in `tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs`

### Implementation for User Story 1

- [X] T024 [US1] Create `OnboardClinicCommand`, response DTO, and validator in `src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/`
- [X] T025 [US1] Implement deterministic schema name generation in `src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandHandler.cs`
- [X] T026 [US1] Implement `TenantProvisioningService` with transactional schema creation, advisory lock, migration execution, audit logging, and rollback compensation in `src/CliniKey.Infrastructure/Persistence/TenantProvisioningService.cs`
- [X] T027 [US1] Implement tenant operational migration application for a new schema in `src/CliniKey.Infrastructure/Persistence/TenantMigrationService.cs`
- [X] T028 [US1] Implement expanded `ClinicRepository` methods in `src/CliniKey.Infrastructure/Persistence/Repositories/ClinicRepository.cs`
- [X] T029 [US1] Implement `OnboardClinicCommandHandler` orchestration in `src/CliniKey.Application/Features/Tenants/Commands/OnboardClinic/OnboardClinicCommandHandler.cs`
- [X] T030 [US1] Create `TenantsController` with `POST /api/v1/tenants/clinics` in `src/CliniKey.API/Controllers/TenantsController.cs`
- [X] T031 [US1] Add shared-schema migration for clinic registry/contact/status/audit data in `src/CliniKey.Infrastructure/Persistence/Migrations/Shared/`
- [X] T032 [US1] Add tenant operational migration baseline in `src/CliniKey.Infrastructure/Persistence/Migrations/Tenant/`

**Checkpoint**: Clinic onboarding is functional and independently testable.

---

## Phase 4: User Story 2 - Isolate Tenant Data via Schema Switching (Priority: P1)

**Goal**: Authenticated tenant requests resolve the JWT tenant, set PostgreSQL search path, and keep concurrent tenant data isolated.

**Independent Test**: Provision two tenants, create patient data under Tenant A, and verify Tenant B queries never return Tenant A rows.

### Tests for User Story 2

- [X] T033 [P] [US2] Add tenant resolution middleware tests for missing, invalid, inactive, and healthy tenants in `tests/CliniKey.Tests/API/TenantResolutionMiddlewareTests.cs`
- [X] T034 [P] [US2] Add EF Core tenant schema switching integration tests in `tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs`
- [X] T035 [P] [US2] Add Dapper tenant schema switching integration tests in `tests/CliniKey.Tests/Infrastructure/TenantDapperConnectionTests.cs`
- [X] T036 [P] [US2] Add concurrent 10-tenant isolation test in `tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs`

### Implementation for User Story 2

- [X] T037 [US2] Implement scoped `TenantContext` in `src/CliniKey.Infrastructure/Persistence/TenantContext.cs`
- [X] T038 [US2] Implement `TenantRegistry` with shared-schema lookup, status/health validation, and short TTL cache in `src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs`
- [X] T039 [US2] Replace header-only tenant resolution with JWT tenant registry resolution in `src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs`
- [X] T040 [US2] Implement EF Core `TenantConnectionInterceptor` to set `search_path` on every opened tenant connection in `src/CliniKey.Infrastructure/Persistence/TenantConnectionInterceptor.cs`
- [X] T041 [US2] Update `IDbConnectionFactory` to support tenant-aware connections in `src/CliniKey.Application/Abstractions/Data/IDbConnectionFactory.cs`
- [X] T042 [US2] Update `DbConnectionFactory` to set tenant `search_path` for Dapper connections in `src/CliniKey.Infrastructure/Persistence/DbConnectionFactory.cs`
- [X] T043 [US2] Register `TenantConnectionInterceptor` with `AppDbContext` in `src/CliniKey.Infrastructure/DependencyInjection.cs`
- [X] T044 [US2] Update existing Dapper query handlers to use tenant-aware connections in `src/CliniKey.Application/Features/Patients/Queries/`, `Appointments/Queries/`, `TreatmentPlans/Queries/`, and `Invoices/Queries/`

**Checkpoint**: Tenant request routing and data isolation work across EF Core, Dapper, and concurrent requests.

---

## Phase 5: User Story 3 - Store Cross-Tenant Data in Shared Schema (Priority: P1)

**Goal**: Clinic, dentist, and clinic-dentist data are stored and queried from `shared` regardless of active tenant schema.

**Independent Test**: With `search_path` set to a tenant schema, query clinics/dentists and verify results come from `shared`.

### Tests for User Story 3

- [X] T045 [P] [US3] Add shared clinic/dentist schema mapping tests in `tests/CliniKey.Tests/Infrastructure/SharedSchemaMappingTests.cs`
- [X] T046 [P] [US3] Add shared dentist query test across two tenant schemas in `tests/CliniKey.Tests/Infrastructure/CrossTenantDentistQueryTests.cs`

### Implementation for User Story 3

- [X] T047 [US3] Move clinic, dentist, and clinic-dentist repository queries to shared schema mappings in `src/CliniKey.Infrastructure/Persistence/Repositories/ClinicRepository.cs`, `DentistRepository.cs`, and `AuthService.cs`
- [X] T048 [US3] Update staff invite flow to create dentist and clinic-dentist rows in `shared` in `src/CliniKey.Infrastructure/Identity/AuthService.cs`
- [X] T049 [US3] Add shared-schema Dapper query support for clinic registry reads in `src/CliniKey.Infrastructure/Persistence/TenantRegistry.cs`

**Checkpoint**: Cross-tenant data remains available and unambiguous under any tenant search path.

---

## Phase 6: User Story 4 - Deactivate and Reactivate a Clinic (Priority: P2)

**Goal**: A platform operator can deactivate/reactivate clinics without deleting schemas or tenant data, and inactive clinic users are blocked.

**Independent Test**: Deactivate a clinic, verify its users receive 403, reactivate it, and verify access returns with data intact.

### Tests for User Story 4

- [X] T050 [P] [US4] Add clinic activation/deactivation domain tests in `tests/CliniKey.Tests/Domain/ClinicTests.cs`
- [X] T051 [P] [US4] Add activation/deactivation handler tests in `tests/CliniKey.Tests/Application/ClinicLifecycleCommandHandlerTests.cs`
- [X] T052 [P] [US4] Add inactive tenant access integration test in `tests/CliniKey.Tests/Infrastructure/TenantLifecycleAccessTests.cs`

### Implementation for User Story 4

- [X] T053 [US4] Create `DeactivateClinicCommand`, validator, and handler in `src/CliniKey.Application/Features/Tenants/Commands/DeactivateClinic/`
- [X] T054 [US4] Create `ActivateClinicCommand`, validator, and handler in `src/CliniKey.Application/Features/Tenants/Commands/ActivateClinic/`
- [X] T055 [US4] Add audit logging for activation and deactivation in `src/CliniKey.Infrastructure/Persistence/TenantProvisioningService.cs`
- [X] T056 [US4] Add `POST /api/v1/tenants/clinics/{clinicId}/deactivate` and `/activate` endpoints to `src/CliniKey.API/Controllers/TenantsController.cs`
- [X] T057 [US4] Ensure tenant resolution returns 403 for inactive or suspended clinics in `src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs`

**Checkpoint**: Tenant lifecycle blocking and restoration work without data loss.

---

## Phase 7: User Story 5 - Complete Clinic Entity with Contact Details (Priority: P2)

**Goal**: Clinic contact details can be retrieved and updated by a platform operator.

**Independent Test**: Retrieve an onboarded clinic and verify phone/address, then update both and verify validation and persistence.

### Tests for User Story 5

- [X] T058 [P] [US5] Add clinic contact update domain tests in `tests/CliniKey.Tests/Domain/ClinicTests.cs`
- [X] T059 [P] [US5] Add clinic contact command/query tests in `tests/CliniKey.Tests/Application/ClinicContactTests.cs`
- [X] T060 [P] [US5] Add clinic list/detail API contract tests in `tests/CliniKey.Tests/API/TenantsControllerTests.cs`

### Implementation for User Story 5

- [X] T061 [US5] Create clinic response DTOs in `src/CliniKey.Application/Features/Tenants/Queries/ClinicResponses.cs`
- [X] T062 [US5] Create `GetClinicByIdQuery` and handler in `src/CliniKey.Application/Features/Tenants/Queries/GetClinicById/`
- [X] T063 [US5] Create `ListClinicsQuery` and handler in `src/CliniKey.Application/Features/Tenants/Queries/ListClinics/`
- [X] T064 [US5] Create `UpdateClinicContactCommand`, validator, and handler in `src/CliniKey.Application/Features/Tenants/Commands/UpdateClinicContact/`
- [X] T065 [US5] Add `GET /api/v1/tenants/clinics`, `GET /api/v1/tenants/clinics/{clinicId}`, and `PUT /api/v1/tenants/clinics/{clinicId}/contact` endpoints to `src/CliniKey.API/Controllers/TenantsController.cs`

**Checkpoint**: Clinic contact details are visible, validated, unique, and updateable.

---

## Phase 8: Tenant Migration Operations

**Purpose**: Support FR-013 and schema health status for existing tenant schemas.

- [X] T066 [P] Add tenant migration service tests for success, drift, lock contention, and failure marking in `tests/CliniKey.Tests/Infrastructure/TenantMigrationServiceTests.cs`
- [X] T067 Create `MigrateTenantSchemasCommand`, response DTOs, validator, and handler in `src/CliniKey.Application/Features/Tenants/Commands/MigrateTenantSchemas/`
- [X] T068 Create `GetTenantSchemaHealthQuery` and handler in `src/CliniKey.Application/Features/Tenants/Queries/GetTenantSchemaHealth/`
- [X] T069 Implement existing-tenant migration enumeration and health updates in `src/CliniKey.Infrastructure/Persistence/TenantMigrationService.cs`
- [X] T070 Add `POST /api/v1/tenants/migrations/apply` and `GET /api/v1/tenants/migrations/status` endpoints to `src/CliniKey.API/Controllers/TenantsController.cs`

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and migration hygiene.

- [X] T071 [P] Update `src/CliniKey.API/CliniKey.API.http` with tenant provisioning and lifecycle sample requests
- [X] T072 [P] Update `specs/003-tenant-provisioning/quickstart.md` if implementation commands or endpoint payloads changed
- [X] T073 Verify generated EF Core migrations do not create operational tables in `public` or `shared` in `src/CliniKey.Infrastructure/Persistence/Migrations/`
- [X] T074 Run `dotnet build CliniKey.slnx` and fix build errors
- [X] T075 Run `dotnet test` and fix failing tests
- [ ] T076 Run the quickstart flow from `specs/003-tenant-provisioning/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **US1 Onboarding**: Depends on Phase 2 and is the MVP.
- **US2 Tenant Isolation**: Depends on Phase 2 and is most useful after US1 can provision schemas.
- **US3 Shared Schema**: Depends on Phase 2 and should be completed before staff/dentist flows are exercised heavily.
- **US4 Lifecycle**: Depends on US1 and US2.
- **US5 Contact Details**: Depends on Phase 2; endpoint work is easiest after US1 creates clinics.
- **Tenant Migration Operations**: Depends on US1 and US2.
- **Polish**: Depends on selected stories and migration operations.

### User Story Dependencies

```text
Phase 1 -> Phase 2 -> US1
                    -> US2
                    -> US3
US1 + US2 -> US4
US1 -> US5
US1 + US2 -> Tenant Migration Operations
All selected phases -> Polish
```

### Parallel Opportunities

- Foundational enum/error/event/config tasks T005-T007, T010-T012, T014-T015 can run in parallel.
- US1 tests T021-T023 can run in parallel before implementation.
- US2 tests T033-T036 can run in parallel before implementation.
- US3 tests T045-T046 can run in parallel before implementation.
- US4 tests T050-T052 can run in parallel before implementation.
- US5 tests T058-T060 can run in parallel before implementation.

---

## Parallel Example: User Story 1

```text
Task: "Add clinic creation/contact validation tests in tests/CliniKey.Tests/Domain/ClinicTests.cs"
Task: "Add onboarding handler tests for success, duplicate phone, and provisioning failure rollback in tests/CliniKey.Tests/Application/OnboardClinicCommandHandlerTests.cs"
Task: "Add provisioning integration tests for schema creation, migration application, and no orphan state in tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add tenant resolution middleware tests for missing, invalid, inactive, and healthy tenants in tests/CliniKey.Tests/API/TenantResolutionMiddlewareTests.cs"
Task: "Add EF Core tenant schema switching integration tests in tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs"
Task: "Add Dapper tenant schema switching integration tests in tests/CliniKey.Tests/Infrastructure/TenantDapperConnectionTests.cs"
Task: "Add concurrent 10-tenant isolation test in tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational work.
3. Complete Phase 3 US1 onboarding.
4. Stop and validate `POST /api/v1/tenants/clinics` plus schema creation/migration.

### Incremental Delivery

1. Add US2 tenant isolation and validate two-tenant data separation.
2. Add US3 shared schema validation for clinic/dentist data.
3. Add US4 lifecycle blocking/reactivation.
4. Add US5 contact update/list/detail endpoints.
5. Add tenant migration operations and final quickstart validation.

### Notes

- Use `Result`/`Result<T>` for all expected failures.
- Use `TimeProvider` and `FakeTimeProvider`; do not use `DateTime.UtcNow`.
- Keep controllers thin and return `result.ToActionResult()` except `CreatedAtAction` for successful POST.
- Keep PostgreSQL identifier quoting centralized in Infrastructure.
- Do not route missing or invalid tenant context to a default schema.
