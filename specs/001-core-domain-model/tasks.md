# Tasks: Core Domain Model

**Input**: Design documents from `/specs/001-core-domain-model/`
**Prerequisites**: plan.md (✅), spec.md (✅)

**Tests**: Tests are included — domain unit tests for value objects and aggregate behaviors, application handler tests, and infrastructure integration tests per constitution principle VI.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: SharedKernel interfaces and Application-layer CQRS abstractions that all features depend on.

- [ ] T001 [P] Create `IUnitOfWork` interface in `src/CliniKey.SharedKernel/Interfaces/IUnitOfWork.cs` — `Task<int> SaveChangesAsync(CancellationToken)`
- [ ] T002 [P] Create `IAuditableEntity` interface in `src/CliniKey.SharedKernel/Interfaces/IAuditableEntity.cs` — `CreatedAtUtc`, `UpdatedAtUtc`
- [ ] T003 [P] Create `ISoftDeletable` interface in `src/CliniKey.SharedKernel/Interfaces/ISoftDeletable.cs` — `IsDeleted`, `DeletedAtUtc`
- [ ] T004 [P] Create `ICommand` and `ICommand<TResponse>` in `src/CliniKey.Application/Abstractions/Messaging/ICommand.cs` — extends `IRequest<Result>` / `IRequest<Result<TResponse>>`
- [ ] T005 [P] Create `IQuery<TResponse>` in `src/CliniKey.Application/Abstractions/Messaging/IQuery.cs` — extends `IRequest<Result<TResponse>>`
- [ ] T006 [P] Create `IDbConnectionFactory` in `src/CliniKey.Application/Abstractions/Data/IDbConnectionFactory.cs` — `DbConnection CreateConnection()`
- [ ] T007 Add FluentValidation.DependencyInjectionExtensions NuGet to `CliniKey.Application.csproj`
- [ ] T008 Add Npgsql.EntityFrameworkCore.PostgreSQL + Dapper NuGet to `CliniKey.Infrastructure.csproj`
- [ ] T009 Add Serilog.AspNetCore NuGet to `CliniKey.API.csproj`
- [ ] T010 Create test project `tests/CliniKey.Tests/CliniKey.Tests.csproj` with xUnit, FluentAssertions, NSubstitute references

---

## Phase 2: Domain Value Objects & Enums (Blocking Prerequisites)

**Purpose**: Value objects and enums that all aggregates depend on. MUST complete before any entity.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T011 [P] Create `Money` value object in `src/CliniKey.Domain/ValueObjects/Money.cs` — `decimal Amount`, `string Currency` ("EGP" default), factory `Create()` returns `Result<Money>`, rejects negative, extends `ValueObject`
- [ ] T012 [P] Create `PhoneNumber` value object in `src/CliniKey.Domain/ValueObjects/PhoneNumber.cs` — Egyptian mobile format (11 digits, starts with "01"), factory `Create()` returns `Result<PhoneNumber>`
- [ ] T013 [P] Create `ToothCode` value object in `src/CliniKey.Domain/ValueObjects/ToothCode.cs` — FDI ISO 3950 validation (permanent: 11–18, 21–28, 31–38, 41–48; deciduous: 51–55, 61–65, 71–75, 81–85)
- [ ] T014 [P] Create `PatientName` value object in `src/CliniKey.Domain/ValueObjects/PatientName.cs` — `FirstName` + `LastName`, non-empty, max 100 chars each
- [ ] T015 [P] Create `LocalizedString` value object in `src/CliniKey.Domain/ValueObjects/LocalizedString.cs` — `En` (required) + `Ar` (optional)
- [ ] T016 [P] Create `Gender` enum in `src/CliniKey.Domain/Enums/Gender.cs` — Male = 1, Female = 2
- [ ] T017 [P] Create `AppointmentStatus` enum in `src/CliniKey.Domain/Enums/AppointmentStatus.cs` — Scheduled, CheckedIn, InProgress, Completed, Cancelled
- [ ] T018 [P] Create `TreatmentPlanStatus` enum in `src/CliniKey.Domain/Enums/TreatmentPlanStatus.cs` — Proposed, Approved, InProgress, Completed, Cancelled
- [ ] T019 [P] Create `TreatmentItemStatus` enum in `src/CliniKey.Domain/Enums/TreatmentItemStatus.cs` — Proposed, InProgress, Completed, Cancelled
- [ ] T020 [P] Create `InvoiceStatus` enum in `src/CliniKey.Domain/Enums/InvoiceStatus.cs` — Draft, Issued, PartiallyPaid, Paid, Voided
- [ ] T021 [P] Create `PaymentMethod` enum in `src/CliniKey.Domain/Enums/PaymentMethod.cs` — Cash, Visa, InstaPay, Fawry, Insurance
- [ ] T022 [P] Create `StaffRole` enum in `src/CliniKey.Domain/Enums/StaffRole.cs` — Admin, Dentist, Receptionist

### Value Object Tests

- [ ] T023 [P] Unit tests for `Money` in `tests/CliniKey.Tests/Domain/MoneyTests.cs` — Create_ValidAmount_ReturnsSuccess, Create_NegativeAmount_ReturnsFailure, equality, default currency EGP
- [ ] T024 [P] Unit tests for `PhoneNumber` in `tests/CliniKey.Tests/Domain/PhoneNumberTests.cs` — Create_ValidEgyptianMobile_ReturnsSuccess, Create_TooShort_ReturnsFailure, Create_InvalidPrefix_ReturnsFailure
- [ ] T025 [P] Unit tests for `ToothCode` in `tests/CliniKey.Tests/Domain/ToothCodeTests.cs` — Create_ValidFDICode_ReturnsSuccess, Create_InvalidCode99_ReturnsFailure, Create_DeciduousTooth_ReturnsSuccess

**Checkpoint**: All value objects and enums compiled and tested. Entity creation can begin.

---

## Phase 3: User Story 1 — Register a New Patient (Priority: P1) 🎯 MVP

**Goal**: A receptionist can register a patient with name, phone, DOB, gender, optional insurance. Phone unique per tenant.

**Independent Test**: POST `/api/v1/patients` creates patient → GET retrieves it → duplicate phone returns 409 error.

### Domain (US1)

- [ ] T026 [US1] Create `Patient` aggregate root in `src/CliniKey.Domain/Entities/Patient.cs` — sealed class, private ctor, factory `Create(PatientName, PhoneNumber, DateOnly, Gender)`, behaviors `UpdatePhone()`, `UpdateInsurance()`, `SoftDelete()`, raises `PatientCreatedEvent`, implements `IAuditableEntity` + `ISoftDeletable`
- [ ] T027 [P] [US1] Create `PatientCreatedEvent` record in `src/CliniKey.Domain/Events/PatientCreatedEvent.cs` — `Guid PatientId`, `DateTime OccurredOnUtc`
- [ ] T028 [P] [US1] Create `PatientErrors` static class in `src/CliniKey.Domain/Errors/PatientErrors.cs` — `DuplicatePhone` (Conflict), `NotFound(Guid)` (NotFound)
- [ ] T029 [P] [US1] Create `IPatientRepository` in `src/CliniKey.Domain/Repositories/IPatientRepository.cs` — `GetByIdAsync`, `ExistsByPhoneAsync`, `Add`

### Application (US1)

- [ ] T030 [US1] Create `PatientResponse` record DTO in `src/CliniKey.Application/DTOs/PatientResponse.cs`
- [ ] T031 [US1] Create `CreatePatientCommand` in `src/CliniKey.Application/Features/Patients/Commands/CreatePatient/CreatePatientCommand.cs` — implements `ICommand<Guid>`
- [ ] T032 [US1] Create `CreatePatientCommandHandler` in `src/CliniKey.Application/Features/Patients/Commands/CreatePatient/CreatePatientCommandHandler.cs` — internal sealed, checks duplicate phone, creates Patient, saves via UnitOfWork
- [ ] T033 [US1] Create `CreatePatientCommandValidator` in `src/CliniKey.Application/Features/Patients/Commands/CreatePatient/CreatePatientCommandValidator.cs` — FluentValidation rules for phone, name, DOB
- [ ] T034 [US1] Create `GetPatientByIdQuery` + `GetPatientByIdQueryHandler` in `src/CliniKey.Application/Features/Patients/Queries/GetPatientById/`
- [ ] T035 [US1] Create `ListPatientsQuery` + `ListPatientsQueryHandler` in `src/CliniKey.Application/Features/Patients/Queries/ListPatients/` — paged, searchable

### Infrastructure (US1)

- [ ] T036 [US1] Create `AppDbContext` in `src/CliniKey.Infrastructure/Persistence/AppDbContext.cs` — extends `DbContext`, implements `IUnitOfWork`, configures `search_path` for tenant schema
- [ ] T037 [US1] Create `PatientConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/PatientConfiguration.cs` — Fluent API, value object conversions (PatientName → OwnsOne, PhoneNumber → conversion), unique phone index, max lengths
- [ ] T038 [US1] Create `PatientRepository` in `src/CliniKey.Infrastructure/Persistence/Repositories/PatientRepository.cs`
- [ ] T039 [US1] Create `UnitOfWork` in `src/CliniKey.Infrastructure/Persistence/UnitOfWork.cs` — dispatches domain events after SaveChanges via MediatR `IPublisher`
- [ ] T040 [US1] Create `DependencyInjection.cs` in `src/CliniKey.Infrastructure/DependencyInjection.cs` — `AddInfrastructure()` extension method registering DbContext, repos, UoW

### API (US1)

- [ ] T041 [P] [US1] Create `ValidationBehavior<TRequest, TResponse>` in `src/CliniKey.Application/Behaviors/ValidationBehavior.cs` — MediatR pipeline behavior, runs FluentValidation before handler
- [ ] T042 [P] [US1] Create `LoggingBehavior<TRequest, TResponse>` in `src/CliniKey.Application/Behaviors/LoggingBehavior.cs` — logs command/query name + execution time
- [ ] T043 [US1] Create `GlobalExceptionMiddleware` in `src/CliniKey.API/Middleware/GlobalExceptionMiddleware.cs` — catches unhandled exceptions, returns ProblemDetails 500
- [ ] T044 [US1] Create `TenantResolutionMiddleware` in `src/CliniKey.API/Middleware/TenantResolutionMiddleware.cs` — reads `X-Tenant-Id` header or JWT claim, sets tenant in scoped service
- [ ] T045 [US1] Create `ResultExtensions` in `src/CliniKey.API/Extensions/ResultExtensions.cs` — `ToActionResult()` maps Error types to HTTP status codes (NotFound→404, Validation→400, Conflict→409)
- [ ] T046 [US1] Create `PatientsController` in `src/CliniKey.API/Controllers/PatientsController.cs` — sealed, injects `ISender`, endpoints: POST, GET/{id}, GET (list), PUT/{id}, DELETE/{id}
- [ ] T047 [US1] Update `Program.cs` in `src/CliniKey.API/Program.cs` — register MediatR, FluentValidation, pipeline behaviors, infrastructure DI, middleware pipeline, Serilog

### Tests (US1)

- [ ] T048 [US1] Unit tests for `Patient` entity in `tests/CliniKey.Tests/Domain/PatientTests.cs` — Create_ValidInput_ReturnsPatient, Create_RaisesDomainEvent, UpdatePhone_ChangesPhone, SoftDelete_SetsFlag
- [ ] T049 [US1] Unit tests for `CreatePatientCommandHandler` in `tests/CliniKey.Tests/Application/CreatePatientCommandHandlerTests.cs` — Handle_ValidCommand_ReturnsGuid, Handle_DuplicatePhone_ReturnsFailure

**Checkpoint**: Patient CRUD is fully functional end-to-end. Can demo registering and retrieving patients. ✅ MVP

---

## Phase 4: User Story 2 — Schedule an Appointment (Priority: P1)

**Goal**: Schedule appointments between patients and dentists with overlapping slot conflict detection.

**Independent Test**: POST `/api/v1/appointments` creates appointment → overlapping slot returns 409 → PATCH status transitions work.

### Domain (US2)

- [X] T050 [US2] Create `Dentist` aggregate root in `src/CliniKey.Domain/Entities/Dentist.cs` — cross-tenant, factory `Create(fullName, specialization, licenseNumber)`
- [X] T051 [P] [US2] Create `Clinic` aggregate root in `src/CliniKey.Domain/Entities/Clinic.cs` — tenant entity, `SchemaName`, `IsActive`
- [X] T052 [P] [US2] Create `ClinicDentist` join entity in `src/CliniKey.Domain/Entities/ClinicDentist.cs` — `ClinicId` + `DentistId`
- [X] T053 [US2] Create `Appointment` aggregate root in `src/CliniKey.Domain/Entities/Appointment.cs` — sealed, factory `Schedule()`, behaviors `CheckIn()`, `Start()`, `Complete()`, `Cancel()`, status state machine validation, concurrency token
- [X] T054 [P] [US2] Create `AppointmentScheduledEvent` in `src/CliniKey.Domain/Events/AppointmentScheduledEvent.cs`
- [X] T055 [P] [US2] Create `AppointmentStatusChangedEvent` in `src/CliniKey.Domain/Events/AppointmentStatusChangedEvent.cs`
- [X] T056 [P] [US2] Create `AppointmentErrors` in `src/CliniKey.Domain/Errors/AppointmentErrors.cs` — `TimeConflict`, `InvalidTransition`, `NotFound`, `PastDate`
- [X] T057 [P] [US2] Create `IAppointmentRepository` in `src/CliniKey.Domain/Repositories/IAppointmentRepository.cs` — `GetByIdAsync`, `HasConflictAsync(dentistId, start, end)`, `Add`
- [X] T058 [P] [US2] Create `IDentistRepository` in `src/CliniKey.Domain/Repositories/IDentistRepository.cs`

### Application (US2)

- [X] T059 [US2] Create `AppointmentResponse` DTO in `src/CliniKey.Application/DTOs/AppointmentResponse.cs`
- [X] T060 [US2] Create `ScheduleAppointmentCommand` + Handler + Validator in `src/CliniKey.Application/Features/Appointments/Commands/ScheduleAppointment/` — checks conflict via repo, creates Appointment
- [X] T061 [US2] Create `ChangeAppointmentStatusCommand` + Handler in `src/CliniKey.Application/Features/Appointments/Commands/ChangeStatus/` — validates state transition
- [X] T062 [US2] Create `GetAppointmentByIdQuery` + Handler in `src/CliniKey.Application/Features/Appointments/Queries/GetAppointmentById/`
- [X] T063 [US2] Create `ListAppointmentsQuery` + Handler in `src/CliniKey.Application/Features/Appointments/Queries/ListAppointments/` — filter by date, dentistId, patientId

### Infrastructure (US2)

- [X] T064 [P] [US2] Create `AppointmentConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs` — concurrency token (`xmin`), indexes on (DentistId, StartTime)
- [X] T065 [P] [US2] Create `DentistConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/DentistConfiguration.cs` — unique LicenseNumber
- [X] T066 [P] [US2] Create `ClinicConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/ClinicConfiguration.cs`
- [X] T067 [US2] Create `AppointmentRepository` in `src/CliniKey.Infrastructure/Persistence/Repositories/AppointmentRepository.cs` — `HasConflictAsync` with overlap query
- [X] T068 [US2] Create `DentistRepository` in `src/CliniKey.Infrastructure/Persistence/Repositories/DentistRepository.cs`
- [X] T069 [US2] Register new DbSets and repos in `AppDbContext` and `DependencyInjection.cs`

### API (US2)

- [X] T070 [US2] Create `AppointmentsController` in `src/CliniKey.API/Controllers/AppointmentsController.cs` — POST, GET/{id}, GET (list), PATCH/{id}/status

### Tests (US2)

- [X] T071 [US2] Unit tests for `Appointment` in `tests/CliniKey.Tests/Domain/AppointmentTests.cs` — Schedule_ValidInput_ReturnsAppointment, CheckIn_FromScheduled_ChangesStatus, CheckIn_FromCompleted_ReturnsFailure, state machine exhaustive
- [X] T072 [US2] Unit tests for `ScheduleAppointmentCommandHandler` in `tests/CliniKey.Tests/Application/ScheduleAppointmentCommandHandlerTests.cs` — Handle_NoConflict_Succeeds, Handle_OverlappingSlot_ReturnsTimeConflict

**Checkpoint**: Appointment scheduling works end-to-end with conflict detection. ✅

---

## Phase 5: User Story 3 — Record a Treatment Plan (Priority: P2)

**Goal**: Dentists create treatment plans with FDI tooth-specific procedures and cost estimates.

**Independent Test**: POST `/api/v1/treatment-plans` with items → GET retrieves with computed total → PATCH approve changes status.

### Domain (US3)

- [X] T073 [US3] Create `TreatmentPlan` aggregate root in `src/CliniKey.Domain/Entities/TreatmentPlan.cs` — factory `Create(patientId, dentistId, items)`, behaviors `Approve()`, `StartItem(itemId)`, `CompleteItem(itemId)`, `Cancel()`, computed `TotalEstimatedCost` from items
- [X] T074 [P] [US3] Create `TreatmentItem` child entity in `src/CliniKey.Domain/Entities/TreatmentItem.cs` — `ToothCode`, `ProcedureName`, `Money EstimatedCost`, `TreatmentItemStatus`
- [X] T075 [P] [US3] Create `TreatmentPlanApprovedEvent` in `src/CliniKey.Domain/Events/TreatmentPlanApprovedEvent.cs`
- [X] T076 [P] [US3] Create `TreatmentPlanErrors` in `src/CliniKey.Domain/Errors/TreatmentPlanErrors.cs` — `EmptyPlan`, `InvalidToothCode`, `InvalidTransition`, `NotFound`
- [X] T077 [P] [US3] Create `ITreatmentPlanRepository` in `src/CliniKey.Domain/Repositories/ITreatmentPlanRepository.cs`

### Application (US3)

- [X] T078 [US3] Create `TreatmentPlanResponse` DTO in `src/CliniKey.Application/DTOs/TreatmentPlanResponse.cs` — includes nested `TreatmentItemResponse` list
- [X] T079 [US3] Create `CreateTreatmentPlanCommand` + Handler + Validator in `src/CliniKey.Application/Features/TreatmentPlans/Commands/CreateTreatmentPlan/` — validates all tooth codes, creates plan with items
- [X] T080 [US3] Create `ApproveTreatmentPlanCommand` + Handler in `src/CliniKey.Application/Features/TreatmentPlans/Commands/ApproveTreatmentPlan/`
- [X] T081 [US3] Create `GetTreatmentPlanByIdQuery` + Handler in `src/CliniKey.Application/Features/TreatmentPlans/Queries/GetTreatmentPlanById/`

### Infrastructure (US3)

- [X] T082 [US3] Create `TreatmentPlanConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/TreatmentPlanConfiguration.cs` — owns many TreatmentItems, ToothCode value conversion, Money conversion
- [X] T083 [US3] Create `TreatmentPlanRepository` in `src/CliniKey.Infrastructure/Persistence/Repositories/TreatmentPlanRepository.cs` — includes Items on load

### API (US3)

- [X] T084 [US3] Create `TreatmentPlansController` in `src/CliniKey.API/Controllers/TreatmentPlansController.cs` — POST, GET/{id}, PATCH/{id}/approve

### Tests (US3)

- [X] T085 [US3] Unit tests for `TreatmentPlan` in `tests/CliniKey.Tests/Domain/TreatmentPlanTests.cs` — Create_WithItems_ComputesTotal, Approve_FromProposed_Succeeds, Create_EmptyItems_ReturnsFailure, InvalidTooth_ReturnsFailure

**Checkpoint**: Treatment plans work with tooth-level procedures and cost tracking. ✅

---

## Phase 6: User Story 4 — Generate an Invoice (Priority: P2)

**Goal**: Generate invoices from treatment plans with per-line 14% VAT calculation and split payments.

**Independent Test**: POST `/api/v1/invoices` from treatment plan → verify VAT math → POST payment → partial/full payment tracking.

### Domain (US4)

- [x] T086 [US4] Create `Invoice` aggregate root in `src/CliniKey.Domain/Entities/Invoice.cs` — factory `CreateFromTreatmentPlan()`, behaviors `Issue()`, `RecordPayment(amount, method)`, `Void()`, computed Subtotal/VatAmount/Total, auto-transitions PartiallyPaid→Paid when fully paid
- [x] T087 [P] [US4] Create `InvoiceLine` child entity in `src/CliniKey.Domain/Entities/InvoiceLine.cs` — `Amount`, `VatRate` (snapshot 0.14m), computed `VatAmount`
- [x] T088 [P] [US4] Create `Payment` child entity in `src/CliniKey.Domain/Entities/Payment.cs` — `Amount`, `PaymentMethod`, `PaidAtUtc`, `ReferenceNumber`
- [x] T089 [P] [US4] Create `InvoicePaidEvent` in `src/CliniKey.Domain/Events/InvoicePaidEvent.cs`
- [x] T090 [P] [US4] Create `InvoiceErrors` in `src/CliniKey.Domain/Errors/InvoiceErrors.cs` — `AlreadyPaid`, `Overpayment`, `NotFound`, `CannotVoid`
- [x] T091 [P] [US4] Create `IInvoiceRepository` in `src/CliniKey.Domain/Repositories/IInvoiceRepository.cs`

### Application (US4)

- [x] T092 [US4] Create `InvoiceResponse` DTO in `src/CliniKey.Application/DTOs/InvoiceResponse.cs` — includes nested lines + payments
- [x] T093 [US4] Create `CreateInvoiceCommand` + Handler + Validator in `src/CliniKey.Application/Features/Invoices/Commands/CreateInvoice/` — loads TreatmentPlan, generates lines with VAT
- [x] T094 [US4] Create `RecordPaymentCommand` + Handler in `src/CliniKey.Application/Features/Invoices/Commands/RecordPayment/` — validates no overpayment
- [x] T095 [US4] Create `GetInvoiceByIdQuery` + Handler in `src/CliniKey.Application/Features/Invoices/Queries/GetInvoiceById/`

### Infrastructure (US4)

- [x] T096 [US4] Create `InvoiceConfiguration` in `src/CliniKey.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs` — owns many InvoiceLines + Payments, Money value conversions
- [x] T097 [US4] Create `InvoiceRepository` in `src/CliniKey.Infrastructure/Persistence/Repositories/InvoiceRepository.cs` — includes Lines + Payments

### API (US4)

- [x] T098 [US4] Create `InvoicesController` in `src/CliniKey.API/Controllers/InvoicesController.cs` — POST, GET/{id}, POST/{id}/payments, PATCH/{id}/issue, PATCH/{id}/void

### Tests (US4)

- [x] T099 [US4] Unit tests for `Invoice` in `tests/CliniKey.Tests/Domain/InvoiceTests.cs` — CreateFromTreatmentPlan_CalculatesVAT, RecordPayment_PartialPayment_StatusPartiallyPaid, RecordPayment_FullPayment_StatusPaid, RecordPayment_Overpayment_ReturnsFailure

**Checkpoint**: Invoices generate from treatment plans with correct VAT (14%) and multi-payment support. ✅

---

## Phase 7: User Story 5 — Manage Clinic Staff (Priority: P3)

**Goal**: Register dentists and receptionists. Dentists linkable to multiple clinics.

**Independent Test**: Create dentist → link to clinic → verify cross-tenant identity.

### Implementation (US5)

- [x] T100 [US5] Add behaviors to `Dentist` (created in T050) — `UpdateSpecialization()`, `UpdateLicenseNumber()`
- [x] T101 [US5] Add behaviors to `Clinic` (created in T051) — `Activate()`, `Deactivate()`, `AddDentist()`, `RemoveDentist()`
- [x] T102 [US5] Create staff-related commands/queries if needed, or seed dev data

**Checkpoint**: Staff management operational. Dentists linkable to multiple clinics. ✅

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that span all user stories.

- [ ] T103 [P] Create `TransactionBehavior<TRequest, TResponse>` in `src/CliniKey.Application/Behaviors/TransactionBehavior.cs` — wraps command handlers in DB transactions
- [ ] T104 Integration test for tenant isolation in `tests/CliniKey.Tests/Infrastructure/TenantIsolationTests.cs` — create two schemas, verify cross-tenant query returns zero rows
- [ ] T105 Verify `dotnet build CliniKey.slnx` — zero warnings, zero errors
- [ ] T106 Verify `dotnet test` — all green
- [ ] T107 Git commit all work on `001-core-domain-model` branch

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)              → no dependencies, start immediately
Phase 2 (VOs & Enums)        → depends on Phase 1
Phase 3 (US1: Patient) 🎯    → depends on Phase 2 — THIS IS THE MVP
Phase 4 (US2: Appointment)   → depends on Phase 2 + T036 (DbContext from US1)
Phase 5 (US3: Treatment)     → depends on Phase 2 + T036
Phase 6 (US4: Invoice)       → depends on Phase 5 (needs TreatmentPlan)
Phase 7 (US5: Staff)         → depends on Phase 4 (Dentist entity created there)
Phase 8 (Polish)             → depends on all above
```

### Within Each User Story

1. Domain entities + events + errors + repository interfaces FIRST
2. Application commands/queries/validators SECOND
3. Infrastructure persistence (config + repo) THIRD
4. API controller FOURTH
5. Tests can run in parallel with implementation

### Parallel Opportunities

- All Phase 1 tasks (T001–T010) are parallelizable
- All Phase 2 value objects and enums (T011–T022) are parallelizable
- All Phase 2 tests (T023–T025) are parallelizable
- Within each user story: domain events, errors, and repo interfaces are parallelizable
- Cross-story: US1 and US2 can overlap once Phase 2 is done (different aggregates)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Value Objects + Enums
3. Complete Phase 3: Patient (US1)
4. **STOP AND VALIDATE**: `dotnet build` + `dotnet test` + manual API test
5. Demo: Register patient → Retrieve patient → Duplicate phone error

### Incremental Delivery

1. Setup + VOs → Foundation ready
2. US1 Patient → **MVP** 🎯
3. US2 Appointment → Scheduling added
4. US3 Treatment → Clinical workflow added
5. US4 Invoice → Revenue tracking added
6. US5 Staff + Polish → Complete feature

---

## Summary

| Metric | Value |
|--------|-------|
| **Total tasks** | 107 |
| **Phase 1 (Setup)** | 10 tasks |
| **Phase 2 (VOs/Enums)** | 15 tasks |
| **Phase 3 (US1: Patient)** | 24 tasks |
| **Phase 4 (US2: Appointment)** | 23 tasks |
| **Phase 5 (US3: Treatment)** | 13 tasks |
| **Phase 6 (US4: Invoice)** | 14 tasks |
| **Phase 7 (US5: Staff)** | 3 tasks |
| **Phase 8 (Polish)** | 5 tasks |
| **Parallel opportunities** | 42 tasks marked [P] |
| **MVP scope** | Phase 1 + 2 + 3 (49 tasks) |
