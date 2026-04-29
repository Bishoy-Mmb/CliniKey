# Implementation Plan: Core Domain Model

**Branch**: `001-core-domain-model` | **Date**: 2026-04-29 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-core-domain-model/spec.md`

## Summary

Build the foundational domain model for CliniKey: 5 aggregate roots (Patient, Appointment, TreatmentPlan, Invoice, Dentist/Clinic), their value objects, domain events, repository contracts, and the EF Core persistence layer. This plan targets the write side (commands) and defines the API contracts for CRUD and lifecycle transitions. All entities are tenant-scoped via PostgreSQL schema-per-tenant.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: MediatR 14.x, FluentValidation, EF Core (Npgsql), Dapper  
**Storage**: PostgreSQL (schema-per-tenant)  
**Testing**: xUnit, FluentAssertions, NSubstitute, Testcontainers  
**Target Platform**: Linux server (Docker) / Windows dev  
**Project Type**: Web API (ASP.NET Core)  
**Performance Goals**: < 200ms p95 for CRUD, appointment conflict detection under concurrency  
**Constraints**: Tenant isolation at DB level, FDI tooth codes, 14% VAT, EGP currency  
**Scale/Scope**: ~50 clinics initial, ~500 patients per clinic, ~20 appointments/day per dentist

## Constitution Check

*GATE: Must pass before implementation.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain Integrity | ✅ Pass | All entities encapsulate behavior via factory methods and state-transition methods |
| II. Spec-Driven Development | ✅ Pass | This plan follows spec 001. Implementation via `/speckit.tasks` + `/speckit.implement` |
| III. Explicit Error Handling | ✅ Pass | All handlers return `Result<T>`. Per-aggregate error classes defined. |
| IV. Tenant Isolation | ✅ Pass | Schema-per-tenant. DbContext sets `search_path`. Integration tests verify isolation. |
| V. Clinical Accuracy | ✅ Pass | FDI tooth codes, `Money` value object, per-line VAT snapshots |
| VI. Test Confidence | ✅ Pass | Domain unit tests, handler unit tests, EF integration tests planned |
| VII. English-First | ✅ Pass | All code in English. `LocalizedString` VO for bilingual DB fields |

## Project Structure

### Documentation (this feature)

```text
specs/001-core-domain-model/
├── spec.md              # Feature specification (done)
├── plan.md              # This file
├── data-model.md        # Entity relationship details (below)
└── tasks.md             # Task breakdown (next step: /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── CliniKey.SharedKernel/
│   ├── Primitives/           # Entity, AggregateRoot, ValueObject, Result, Error, IDomainEvent (done)
│   └── Interfaces/           # IUnitOfWork, IAuditableEntity, ISoftDeletable (this plan)
│
├── CliniKey.Domain/
│   ├── Entities/
│   │   ├── Patient.cs
│   │   ├── Appointment.cs
│   │   ├── TreatmentPlan.cs
│   │   ├── TreatmentItem.cs
│   │   ├── Invoice.cs
│   │   ├── InvoiceLine.cs
│   │   ├── Payment.cs
│   │   ├── Dentist.cs
│   │   ├── Clinic.cs
│   │   └── ClinicDentist.cs
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   ├── PhoneNumber.cs
│   │   ├── ToothCode.cs
│   │   ├── PatientName.cs
│   │   └── LocalizedString.cs
│   ├── Enums/
│   │   ├── Gender.cs
│   │   ├── AppointmentStatus.cs
│   │   ├── TreatmentPlanStatus.cs
│   │   ├── TreatmentItemStatus.cs
│   │   ├── InvoiceStatus.cs
│   │   ├── PaymentMethod.cs
│   │   └── StaffRole.cs
│   ├── Events/
│   │   ├── PatientCreatedEvent.cs
│   │   ├── AppointmentScheduledEvent.cs
│   │   ├── AppointmentStatusChangedEvent.cs
│   │   ├── TreatmentPlanApprovedEvent.cs
│   │   └── InvoicePaidEvent.cs
│   ├── Repositories/
│   │   ├── IPatientRepository.cs
│   │   ├── IAppointmentRepository.cs
│   │   ├── ITreatmentPlanRepository.cs
│   │   ├── IInvoiceRepository.cs
│   │   └── IDentistRepository.cs
│   └── Errors/
│       ├── PatientErrors.cs
│       ├── AppointmentErrors.cs
│       ├── TreatmentPlanErrors.cs
│       └── InvoiceErrors.cs
│
├── CliniKey.Application/
│   ├── Abstractions/
│   │   ├── Messaging/
│   │   │   ├── ICommand.cs
│   │   │   ├── ICommandHandler.cs
│   │   │   ├── IQuery.cs
│   │   │   └── IQueryHandler.cs
│   │   └── Data/
│   │       └── IDbConnectionFactory.cs
│   ├── Features/
│   │   ├── Patients/
│   │   │   ├── Commands/
│   │   │   │   ├── CreatePatient/
│   │   │   │   │   ├── CreatePatientCommand.cs
│   │   │   │   │   ├── CreatePatientCommandHandler.cs
│   │   │   │   │   └── CreatePatientCommandValidator.cs
│   │   │   │   └── UpdatePatient/
│   │   │   │       └── ...
│   │   │   └── Queries/
│   │   │       ├── GetPatientById/
│   │   │       │   ├── GetPatientByIdQuery.cs
│   │   │       │   └── GetPatientByIdQueryHandler.cs
│   │   │       └── ListPatients/
│   │   │           └── ...
│   │   ├── Appointments/
│   │   │   ├── Commands/
│   │   │   │   └── ScheduleAppointment/
│   │   │   │       └── ...
│   │   │   └── Queries/
│   │   │       └── ...
│   │   ├── TreatmentPlans/
│   │   │   └── ...
│   │   └── Invoices/
│   │       └── ...
│   ├── DTOs/
│   │   ├── PatientResponse.cs
│   │   ├── AppointmentResponse.cs
│   │   ├── TreatmentPlanResponse.cs
│   │   └── InvoiceResponse.cs
│   └── Behaviors/
│       ├── ValidationBehavior.cs
│       ├── LoggingBehavior.cs
│       └── TransactionBehavior.cs
│
├── CliniKey.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/
│   │   │   ├── PatientConfiguration.cs
│   │   │   ├── AppointmentConfiguration.cs
│   │   │   ├── TreatmentPlanConfiguration.cs
│   │   │   ├── InvoiceConfiguration.cs
│   │   │   └── DentistConfiguration.cs
│   │   └── Repositories/
│   │       ├── PatientRepository.cs
│   │       ├── AppointmentRepository.cs
│   │       ├── TreatmentPlanRepository.cs
│   │       ├── InvoiceRepository.cs
│   │       └── DentistRepository.cs
│   └── DependencyInjection.cs
│
└── CliniKey.API/
    ├── Controllers/
    │   ├── PatientsController.cs
    │   ├── AppointmentsController.cs
    │   ├── TreatmentPlansController.cs
    │   └── InvoicesController.cs
    ├── Middleware/
    │   ├── GlobalExceptionMiddleware.cs
    │   └── TenantResolutionMiddleware.cs
    ├── Extensions/
    │   └── ResultExtensions.cs        # Result → IActionResult/ProblemDetails mapping
    └── Program.cs                     # DI registration, pipeline setup

tests/
└── CliniKey.Tests/
    ├── Domain/
    │   ├── PatientTests.cs
    │   ├── AppointmentTests.cs
    │   ├── MoneyTests.cs
    │   ├── ToothCodeTests.cs
    │   └── PhoneNumberTests.cs
    ├── Application/
    │   ├── CreatePatientCommandHandlerTests.cs
    │   └── ScheduleAppointmentCommandHandlerTests.cs
    └── Infrastructure/
        └── TenantIsolationTests.cs
```

**Structure Decision**: Multi-project Clean Architecture (already scaffolded). Source code in `src/`, tests in `tests/`. Each feature gets a vertical slice folder under `Application/Features/`.

## Data Model

### Aggregate: Patient

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| Name | `PatientName` (VO) | Required, First + Last |
| Phone | `PhoneNumber` (VO) | Required, unique per tenant, 11 digits |
| DateOfBirth | `DateOnly` | Required |
| Gender | `Gender` (enum) | Male, Female |
| MedicalNotes | `string?` | Optional, max 2000 chars |
| InsuranceProvider | `string?` | Optional, max 200 chars |
| InsurancePolicyNumber | `string?` | Optional, max 50 chars |
| InsuranceCoveragePercent | `decimal?` | Optional, 0-100 |
| IsDeleted | `bool` | Soft delete flag |
| CreatedAtUtc | `DateTime` | From AggregateRoot |
| UpdatedAtUtc | `DateTime?` | From AggregateRoot |

**Behaviors**: `Create()`, `UpdatePhone()`, `UpdateInsurance()`, `SoftDelete()`  
**Events**: `PatientCreatedEvent`  
**Errors**: `PatientErrors.DuplicatePhone`, `PatientErrors.NotFound`

### Aggregate: Appointment

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| PatientId | `Guid` (FK) | Required |
| DentistId | `Guid` (FK) | Required |
| StartTime | `DateTime` | Required, future dates only |
| DurationMinutes | `int` | Required, min 15, max 240 |
| Status | `AppointmentStatus` | Scheduled → CheckedIn → InProgress → Completed / Cancelled |
| Notes | `string?` | Optional, max 1000 chars |
| ConcurrencyToken | `byte[]` | EF Core row version |

**Behaviors**: `Schedule()`, `CheckIn()`, `Start()`, `Complete()`, `Cancel()`  
**Events**: `AppointmentScheduledEvent`, `AppointmentStatusChangedEvent`  
**Errors**: `AppointmentErrors.TimeConflict`, `AppointmentErrors.InvalidTransition`

### Aggregate: TreatmentPlan

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| PatientId | `Guid` (FK) | Required |
| DentistId | `Guid` (FK) | Required |
| Status | `TreatmentPlanStatus` | Proposed → Approved → InProgress → Completed / Cancelled |
| Items | `List<TreatmentItem>` | At least one item required |
| TotalEstimatedCost | `Money` (computed) | Sum of item costs |

**Child Entity: TreatmentItem**

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| TreatmentPlanId | `Guid` (FK) | Required |
| Tooth | `ToothCode` (VO) | Required, valid FDI code |
| ProcedureName | `string` | Required, max 200 chars |
| EstimatedCost | `Money` (VO) | Required, positive |
| Status | `TreatmentItemStatus` | Proposed / InProgress / Completed / Cancelled |

**Behaviors**: `Create()`, `Approve()`, `StartItem()`, `CompleteItem()`, `Cancel()`  
**Events**: `TreatmentPlanApprovedEvent`  
**Errors**: `TreatmentPlanErrors.InvalidToothCode`, `TreatmentPlanErrors.EmptyPlan`

### Aggregate: Invoice

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| PatientId | `Guid` (FK) | Required |
| TreatmentPlanId | `Guid?` (FK) | Optional (walkIn invoices) |
| Status | `InvoiceStatus` | Draft → Issued → PartiallyPaid → Paid / Voided |
| Lines | `List<InvoiceLine>` | At least one line |
| Subtotal | `Money` (computed) | Sum of line amounts |
| VatAmount | `Money` (computed) | Sum of line VAT amounts |
| Total | `Money` (computed) | Subtotal + VatAmount |
| Payments | `List<Payment>` | Zero or more |

**Child Entity: InvoiceLine**

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| InvoiceId | `Guid` (FK) | Required |
| Description | `string` | Required, max 300 chars |
| Amount | `Money` (VO) | Required, positive |
| VatRate | `decimal` | Snapshot at creation (0.14) |
| VatAmount | `Money` (computed) | Amount × VatRate |

**Child Entity: Payment**

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| InvoiceId | `Guid` (FK) | Required |
| Amount | `Money` (VO) | Required, positive |
| Method | `PaymentMethod` | Cash, Visa, InstaPay, Fawry, Insurance |
| PaidAtUtc | `DateTime` | Required |
| ReferenceNumber | `string?` | Optional, for electronic payments |

**Behaviors**: `CreateFromTreatmentPlan()`, `Issue()`, `RecordPayment()`, `Void()`  
**Events**: `InvoicePaidEvent`  
**Errors**: `InvoiceErrors.AlreadyPaid`, `InvoiceErrors.Overpayment`

### Entity: Dentist (Cross-Tenant)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| FullName | `string` | Required, max 200 chars |
| Specialization | `string` | Required, max 100 chars |
| LicenseNumber | `string` | Required, unique, max 50 chars |

### Entity: Clinic (Tenant)

| Property | Type | Constraints |
|----------|------|-------------|
| Id | `Guid` (PK) | Generated on create |
| Name | `string` | Required, max 200 chars |
| SchemaName | `string` | Required, unique, auto-generated |
| Phone | `PhoneNumber` (VO) | Required |
| Address | `string` | Required, max 500 chars |
| IsActive | `bool` | Default true |

## API Contracts

### Patients API

| Method | Endpoint | Request Body | Response | Status |
|--------|----------|-------------|----------|--------|
| POST | `/api/v1/patients` | `CreatePatientRequest` | `Guid` | 201 |
| GET | `/api/v1/patients/{id}` | — | `PatientResponse` | 200 |
| GET | `/api/v1/patients` | `?page&size&search` | `PagedList<PatientResponse>` | 200 |
| PUT | `/api/v1/patients/{id}` | `UpdatePatientRequest` | — | 204 |
| DELETE | `/api/v1/patients/{id}` | — | — | 204 (soft) |

### Appointments API

| Method | Endpoint | Request Body | Response | Status |
|--------|----------|-------------|----------|--------|
| POST | `/api/v1/appointments` | `ScheduleAppointmentRequest` | `Guid` | 201 |
| GET | `/api/v1/appointments/{id}` | — | `AppointmentResponse` | 200 |
| GET | `/api/v1/appointments` | `?date&dentistId&patientId` | `List<AppointmentResponse>` | 200 |
| PATCH | `/api/v1/appointments/{id}/status` | `{ status: "CheckedIn" }` | — | 204 |

### Treatment Plans API

| Method | Endpoint | Request Body | Response | Status |
|--------|----------|-------------|----------|--------|
| POST | `/api/v1/treatment-plans` | `CreateTreatmentPlanRequest` | `Guid` | 201 |
| GET | `/api/v1/treatment-plans/{id}` | — | `TreatmentPlanResponse` | 200 |
| PATCH | `/api/v1/treatment-plans/{id}/approve` | — | — | 204 |

### Invoices API

| Method | Endpoint | Request Body | Response | Status |
|--------|----------|-------------|----------|--------|
| POST | `/api/v1/invoices` | `CreateInvoiceRequest` | `Guid` | 201 |
| GET | `/api/v1/invoices/{id}` | — | `InvoiceResponse` | 200 |
| POST | `/api/v1/invoices/{id}/payments` | `RecordPaymentRequest` | `Guid` | 201 |
| PATCH | `/api/v1/invoices/{id}/issue` | — | — | 204 |
| PATCH | `/api/v1/invoices/{id}/void` | — | — | 204 |

## Migration Strategy

1. **Initial migration**: Create all tables in a `public` schema as a template.
2. **Tenant provisioning**: When a new clinic registers, run `CREATE SCHEMA clinic_{id}` and apply the template migration to the new schema.
3. **Schema resolution**: `TenantResolutionMiddleware` reads tenant from JWT or header, sets `search_path` on the `DbContext`.
4. **Cross-tenant data** (Dentist, Clinic): Stored in the `shared` schema, accessible from all tenant schemas.

## Complexity Tracking

> No constitution violations. All patterns are justified by the spec requirements.

| Pattern | Why Needed | Simpler Alternative Rejected Because |
|---------|------------|--------------------------------------|
| Schema-per-tenant | FR-001 requires DB-level isolation | Row-level filtering is one WHERE clause away from a data breach |
| CQRS | Read/write patterns differ (list vs. transactional write) | Direct repo calls don't support pipeline behaviors |
| Result pattern | FR-013 requires explicit error paths for state transitions | Exceptions are invisible and easily swallowed |
| Domain events | FR-013 requires decoupled side effects on state transitions | Direct calls would create circular dependencies between aggregates |
