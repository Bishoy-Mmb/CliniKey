# CliniKey Constitution

## Core Principles

### I. Domain Integrity Above All

The domain layer is the heart of CliniKey. It models the real-world clinical workflow of Egyptian dental clinics — not the database schema, not the API shape, not the UI. Every design decision flows outward from the domain.

- Entities encapsulate behavior. No anemic models — a `Patient` knows how to update its own medical history; an `Appointment` knows how to reschedule itself.
- Value objects enforce validity at construction. A `ToothCode` that violates FDI notation cannot exist in memory.
- Aggregate boundaries are consistency boundaries. If two things must be transactionally consistent, they belong in the same aggregate. If not, they communicate via domain events.
- The domain layer has zero infrastructure dependencies. It compiles with nothing but the SharedKernel.

### II. Spec-Driven Development (NON-NEGOTIABLE)

Every new feature, entity, or user-facing capability begins as a specification — not as code. Specifications capture *what* and *why* before *how*.

- Follow the SDD pipeline: **specify → clarify → plan → tasks → implement**.
- No raw C# files for new features without a spec artifact in `.specify/specs/`.
- Specs are living documents. When requirements change, update the spec first, then propagate changes to code.
- The `skill.md` file is the engineering source of truth. The constitution governs *principles*; the skill file governs *standards*.

### III. Explicit Error Handling Over Exceptions

Business rule violations are expected outcomes, not exceptional circumstances. The system uses the Result pattern to make error paths visible and enforceable.

- Commands and queries return `Result` or `Result<T>` — never throw for business logic.
- Each aggregate defines its own static error class (e.g., `PatientErrors`, `AppointmentErrors`).
- Exceptions are reserved for truly unexpected failures (database down, network timeout) and are caught only at the middleware boundary.
- Every error has a structured code (`Entity.ErrorType`) for programmatic handling and a human-readable description for logging/display.

### IV. Tenant Isolation Is a Security Boundary

CliniKey is a multi-tenant SaaS. Each clinic is a tenant. Data leakage between tenants is a **critical security failure**, not a bug.

- Schema-per-tenant in PostgreSQL. Every query executes within the tenant's schema.
- Tenant resolution happens once per request via middleware, before any business logic runs.
- No query may execute without a resolved tenant context. If tenant resolution fails, the request is rejected immediately (401/403).
- Integration tests must explicitly verify that Tenant A cannot access Tenant B's data.

### V. Clinical Accuracy Is Non-Negotiable

CliniKey handles patient health data and financial records. Incorrect data can affect treatment decisions and billing disputes.

- Tooth numbering follows the FDI (ISO 3950) standard. No custom numbering schemes.
- Financial calculations use `decimal` types, never `float` or `double`. All money is represented via a `Money` value object with explicit currency (`EGP`).
- VAT rate (14%) is stored per invoice line at the time of creation — never recalculated retroactively.
- Audit trails: all mutations to patient records, appointments, and invoices must be traceable (who changed what, when).

### VI. Test Confidence Over Test Coverage

Tests exist to give confidence that the system behaves correctly under real-world conditions — not to hit an arbitrary coverage number.

- Domain logic: unit tests with no database dependency. Test entity behaviors, value object validation, and aggregate invariants.
- Application handlers: unit tests with mocked repositories. Test orchestration, not persistence.
- Infrastructure: integration tests with Testcontainers (real PostgreSQL). Test that EF configurations and queries actually work.
- API: integration tests via `WebApplicationFactory`. Test the full HTTP pipeline including auth, tenant resolution, and serialization.
- Naming convention: `MethodUnderTest_Scenario_ExpectedResult`.

### VII. English-First, Arabic-Ready

CliniKey targets the Egyptian market, but the codebase and primary interface are in English.

- All code, comments, variable names, commit messages, and documentation are in English.
- API responses default to English. Arabic is provided via `ar-EG` localization when requested.
- User-facing strings that require bilingual support use a `LocalizedString` value object (with `En` and `Ar` properties) stored in the database.
- Resource files (`.resx`) provide Arabic translations for validation messages, error descriptions, and UI labels.
- RTL layout is a frontend concern — the API is language-agnostic and returns `Content-Language` headers.

## Architectural Constraints

- **Clean Architecture**: dependencies point inward. API → Infrastructure → Application → Domain → SharedKernel. Never the reverse.
- **CQRS via MediatR**: commands mutate state, queries read state. They never share handler classes.
- **Vertical Slice Features**: each feature (Patients, Appointments, Treatments, Invoices) is a self-contained folder under `Application/Features/` with its own commands, queries, validators, and event handlers.
- **No framework bleed**: domain entities must not reference EF Core, ASP.NET, or any infrastructure framework. The domain is a plain C# library.
- **Single writer principle**: only one command handler may modify a given aggregate per transaction. Cross-aggregate side effects happen via domain events, processed asynchronously or in a separate unit of work.

## Quality Gates

Every piece of work must satisfy these gates before merge:

1. **Spec exists**: new features have a corresponding `.specify/specs/` entry.
2. **Build passes**: `dotnet build CliniKey.slnx` — zero warnings, zero errors.
3. **Tests pass**: `dotnet test` — all green.
4. **At least one test**: every behavioral change ships with at least one test.
5. **No domain leakage**: domain entities are never returned from API endpoints. DTOs only.
6. **Tenant-scoped**: any new data access is verified to respect tenant boundaries.
7. **Skill compliance**: code follows the patterns in `skill.md` (naming, Result pattern, sealed handlers, etc.).

## Governance

- This constitution supersedes ad-hoc decisions. When in doubt, refer here.
- Amendments require: (1) documented rationale, (2) update to this file, (3) corresponding updates to `skill.md` if standards are affected.
- The `skill.md` file handles tactical standards (naming, folder structure, approved packages). This constitution handles strategic principles (why we make certain choices).
- Both files must remain consistent. A contradiction between them is a defect to be resolved immediately.

**Version**: 1.0.0 | **Ratified**: 2026-04-29 | **Last Amended**: 2026-04-29
