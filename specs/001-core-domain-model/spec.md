# Feature Specification: Core Domain Model

**Feature Branch**: `001-core-domain-model`  
**Created**: 2026-04-29  
**Status**: Draft  
**Input**: User description: "Define the foundational domain model for CliniKey — a multi-tenant Dental Management SaaS targeting the Egyptian market. This spec covers the core aggregates, entities, value objects, and their relationships that form the backbone of all future features."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register a New Patient (Priority: P1)

A receptionist at a dental clinic opens CliniKey and registers a new patient. She enters the patient's full name, phone number, date of birth, gender, and optional insurance details. The system creates a patient record scoped to the clinic's tenant.

**Why this priority**: Without patients, nothing else in the system works — appointments, treatments, invoices all depend on a patient record existing.

**Independent Test**: Can be fully tested by creating a patient via API and retrieving it. Delivers immediate value as the first data entry point.

**Acceptance Scenarios**:

1. **Given** a receptionist is logged in with a resolved tenant, **When** she submits a valid patient registration with name "Ahmed Hassan" and phone "01012345678", **Then** the system creates a patient record with a unique ID, stores it in the tenant's schema, and returns the ID.
2. **Given** a patient with phone "01012345678" already exists in this tenant, **When** the receptionist submits another patient with the same phone, **Then** the system returns a `Patient.DuplicatePhone` error.
3. **Given** the receptionist enters a phone number with fewer than 11 digits, **When** she submits the form, **Then** the system returns a validation error before reaching the domain layer.

---

### User Story 2 - Schedule an Appointment (Priority: P1)

A receptionist selects a patient and a dentist, picks a date/time slot, and schedules an appointment. The system checks for conflicts and creates the appointment.

**Why this priority**: Appointment scheduling is the primary daily workflow in any dental clinic. It is co-equal with patient registration as the core use case.

**Independent Test**: Can be tested by creating a patient, a dentist, and scheduling an appointment between them. Verifiable by querying the appointment back.

**Acceptance Scenarios**:

1. **Given** patient "Ahmed" and dentist "Dr. Mona" exist, **When** the receptionist schedules a 30-minute appointment for 2026-05-01 at 10:00 AM, **Then** the system creates an appointment with status `Scheduled` and raises an `AppointmentScheduledEvent`.
2. **Given** Dr. Mona already has an appointment at 10:00 AM on 2026-05-01, **When** another appointment is scheduled overlapping that slot, **Then** the system returns an `Appointment.TimeConflict` error.
3. **Given** an appointment exists with status `Scheduled`, **When** the patient arrives and the receptionist checks them in, **Then** the appointment status changes to `CheckedIn`.

---

### User Story 3 - Record a Treatment Plan (Priority: P2)

After examining a patient, a dentist creates a treatment plan. The plan specifies which teeth need treatment, what procedures will be performed, and the estimated cost for each. A treatment plan can have multiple treatment items.

**Why this priority**: Treatment planning is clinically critical but depends on patient and appointment already existing. It directly feeds into invoicing.

**Independent Test**: Can be tested by creating a treatment plan with two procedures for specific teeth and verifying the total cost calculation.

**Acceptance Scenarios**:

1. **Given** patient "Ahmed" has a completed appointment, **When** Dr. Mona creates a treatment plan with a "Root Canal on tooth 16" (1,500 EGP) and "Composite Filling on tooth 26" (500 EGP), **Then** the system creates a plan with two items, total estimated cost of 2,000 EGP, and status `Proposed`.
2. **Given** a treatment plan exists with status `Proposed`, **When** the patient approves it, **Then** the status changes to `Approved` and a `TreatmentPlanApprovedEvent` is raised.
3. **Given** a treatment item references tooth "99", **When** the plan is submitted, **Then** the system returns a validation error because "99" is not a valid FDI tooth code.

---

### User Story 4 - Generate an Invoice (Priority: P2)

When treatment is completed (or partially completed), the receptionist generates an invoice for the patient. The invoice pulls line items from the treatment plan, applies the VAT rate, and supports multiple payment methods.

**Why this priority**: Revenue collection is a core business requirement. Invoices must be accurate and auditable. Depends on treatment plan existing.

**Independent Test**: Can be tested by completing a treatment item, generating an invoice, and verifying line items, VAT calculation, and total.

**Acceptance Scenarios**:

1. **Given** a treatment plan with an "Approved" root canal (1,500 EGP), **When** the receptionist generates an invoice, **Then** the invoice contains a line item of 1,500 EGP, VAT at 14% (210 EGP), and total of 1,710 EGP.
2. **Given** an invoice with total 1,710 EGP, **When** the patient pays 1,000 EGP in cash and 710 EGP via InstaPay, **Then** the invoice status changes to `Paid` and two payment records are created.
3. **Given** the patient has insurance with 50% coverage, **When** the invoice is generated, **Then** the system calculates the insurance portion (855 EGP) and the patient portion (855 EGP).

---

### User Story 5 - Manage Clinic Staff (Priority: P3)

A clinic admin registers dentists and receptionists who work at the clinic. Each staff member has a role, and dentists have a specialization. A dentist may work at multiple clinics.

**Why this priority**: Staff management is required for appointment scheduling and access control, but can be seeded/hardcoded for initial development of P1/P2 stories.

**Independent Test**: Can be tested by creating a dentist with specialization "Endodontics" and verifying the record.

**Acceptance Scenarios**:

1. **Given** a clinic admin is logged in, **When** they register Dr. Mona as a dentist with specialization "Orthodontics", **Then** the system creates a staff record linked to the tenant.
2. **Given** Dr. Mona is registered in Clinic A, **When** Clinic B also registers her (same national ID), **Then** the system links her to both tenants without duplicating the identity record.

---

### Edge Cases

- What happens when a patient is soft-deleted but has future appointments? → Future appointments are cancelled, raising `AppointmentCancelledEvent` for each.
- How does the system handle concurrent appointment bookings for the same slot? → Optimistic concurrency via EF Core concurrency tokens on the appointment time slot.
- What if a treatment plan is partially completed when the patient requests cancellation? → Completed items remain billable; only `Proposed`/`Approved` items are cancelled.
- What happens when VAT rate changes nationally? → New invoices use the new rate. Existing invoices retain their stored rate (per-line snapshot).
- How does the system handle a dentist being removed from a clinic while they have future appointments? → Appointments are flagged for reassignment, not auto-cancelled.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support multi-tenant data isolation at the database schema level.
- **FR-002**: System MUST register patients with: full name, phone (unique per tenant), date of birth, gender, and optional insurance details.
- **FR-003**: System MUST enforce FDI (ISO 3950) tooth numbering for all treatment references.
- **FR-004**: System MUST schedule appointments with conflict detection (no overlapping slots per dentist).
- **FR-005**: System MUST support appointment lifecycle: `Scheduled → CheckedIn → InProgress → Completed → Cancelled`.
- **FR-006**: System MUST create treatment plans with multiple line items, each referencing a tooth and procedure.
- **FR-007**: System MUST support treatment plan lifecycle: `Proposed → Approved → InProgress → Completed → Cancelled`.
- **FR-008**: System MUST generate invoices with per-line VAT calculation at the current rate (14%).
- **FR-009**: System MUST support split payments across multiple methods (Cash, Visa, InstaPay, Fawry, Insurance).
- **FR-010**: System MUST support invoice lifecycle: `Draft → Issued → PartiallyPaid → Paid → Voided`.
- **FR-011**: System MUST support staff roles: `Admin`, `Dentist`, `Receptionist`.
- **FR-012**: System MUST support soft deletion for patients, with cascading effects on future appointments.
- **FR-013**: System MUST raise domain events for key state transitions (patient created, appointment scheduled, treatment approved, invoice paid).
- **FR-014**: System MUST store all monetary values as `decimal` with `EGP` currency using a `Money` value object.
- **FR-015**: System MUST support bilingual content (English primary, Arabic secondary) for patient-facing fields via a `LocalizedString` value object.

### Key Entities

- **Clinic (Tenant)**: The multi-tenancy boundary. Each clinic is an independent data silo. Key attributes: name, address, phone, subscription plan, active status.
- **Patient**: The central clinical entity. Belongs to exactly one tenant. Key attributes: name, phone, date of birth, gender, medical notes, insurance info. Unique constraint: phone per tenant.
- **Dentist**: A clinical staff member who performs treatments. May belong to multiple clinics. Key attributes: name, specialization, license number.
- **Appointment**: A scheduled meeting between a patient and a dentist. Key attributes: date/time, duration, status, notes. Constraint: no overlapping appointments per dentist.
- **TreatmentPlan**: A collection of proposed procedures for a patient. Created by a dentist. Key attributes: status, total estimated cost, list of treatment items.
- **TreatmentItem**: A single procedure within a treatment plan. Key attributes: tooth code (FDI), procedure name, estimated cost, status.
- **Invoice**: A financial document for completed/approved treatments. Key attributes: line items, subtotal, VAT amount, total, status, payment records.
- **InvoiceLine**: A single billable item on an invoice. Key attributes: description, amount, VAT rate, VAT amount.
- **Payment**: A partial or full payment against an invoice. Key attributes: amount, method (Cash/Visa/InstaPay/Fawry/Insurance), date, reference number.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A patient can be registered and retrieved within a single API round-trip (< 200ms p95).
- **SC-002**: Appointment conflict detection correctly prevents 100% of overlapping bookings in concurrent scenarios.
- **SC-003**: Invoice VAT calculation matches manual calculation to the piaster (0.01 EGP precision).
- **SC-004**: Tenant isolation is verified: a query from Tenant A returns zero rows from Tenant B's data, tested explicitly in integration tests.
- **SC-005**: All five aggregate lifecycles (Patient, Appointment, TreatmentPlan, Invoice, Staff) have full unit test coverage for state transitions.
- **SC-006**: The domain layer compiles with zero references to EF Core, ASP.NET, or any infrastructure package.

## Assumptions

- The frontend is out of scope for this spec. This covers the domain model and API layer only.
- Authentication (login/registration) will be handled in a separate spec. For now, tenant and user identity are assumed resolved by middleware.
- SMS/email notifications for appointment reminders are out of scope for this spec.
- Insurance claim submission to providers is out of scope. We only track coverage percentage and calculate patient vs. insurance portions.
- A single appointment maps to one dentist and one patient. Group appointments are not supported in v1.
- Subscription/billing management (the SaaS subscription model for clinics) is a separate bounded context, not covered here.

## Review & Acceptance Checklist

- [x] All user stories have clear acceptance scenarios with Given/When/Then
- [x] Priorities are assigned and justified
- [x] Each story is independently testable
- [x] Edge cases are identified and have defined behavior
- [x] Key entities and their relationships are documented
- [x] Egyptian market specifics are addressed (EGP, VAT, FDI, payment methods)
- [x] Multi-tenancy implications are explicitly stated
- [x] Success criteria are measurable and technology-agnostic
- [x] Assumptions clearly scope what is and isn't included
