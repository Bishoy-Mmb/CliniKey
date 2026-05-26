# Feature Specification: Tenant Provisioning

**Feature Branch**: `003-tenant-provisioning`  
**Created**: 2026-05-21  
**Status**: Draft  
**Input**: User description: "Implement the tenant provisioning lifecycle for CliniKey: the automated process of creating, configuring, and managing tenant/practice isolation. Tenant/Practice is the isolation boundary and owns schema name, provisioning status, schema health, and current migration. Clinic is a branch/location under a tenant. V1 still onboards one clinic, but internally creates a tenant/practice plus the first clinic branch."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Onboard a New Tenant/Practice With First Clinic (Priority: P1)

A platform operator registers a new practice on the CliniKey platform. They provide the initial clinic branch name, phone number, and address. The system creates the tenant/practice record, creates the first clinic branch under that tenant, generates a unique schema name from the tenant ID, provisions a dedicated PostgreSQL schema, applies all required table migrations to the new schema, and marks the tenant and clinic as active and ready for use.

**Why this priority**: Without tenant provisioning, no clinic can operate with data isolation. This is the foundational prerequisite for all multi-tenant operations. Every other feature — patient management, appointments, treatments, invoices — depends on a provisioned tenant schema existing.

**Independent Test**: Can be fully tested by calling the tenant onboarding endpoint, verifying the tenant and first clinic records are created in the shared schema, and confirming that the dedicated PostgreSQL schema exists with all expected tables.

**Acceptance Scenarios**:

1. **Given** a platform operator provides clinic branch name "Cairo Dental Center", phone "01112345678", and address "15 Tahrir St, Cairo", **When** the onboarding request is submitted, **Then** the system creates a tenant/practice record and first clinic record with unique IDs, generates a schema name (e.g., `tenant_<short_id>`) from the tenant ID, creates the PostgreSQL schema, applies all table migrations, and returns both tenant ID and clinic ID.
2. **Given** a clinic with phone "01112345678" already exists, **When** another onboarding request uses the same phone, **Then** the system returns a `Tenant.DuplicatePhone` error.
3. **Given** the database is temporarily unreachable during schema creation, **When** the onboarding request is processed, **Then** the system rolls back any partial state (no orphaned tenant or clinic record without a schema) and returns a clear error indicating the operation failed.
4. **Given** a clinic is successfully onboarded, **When** the system is queried for the new schema's tables, **Then** the schema contains all operational tables (patients, appointments, treatment_plans, treatment_items, invoices, invoice_lines, payments) matching the current domain model.

---

### User Story 2 - Isolate Tenant Data via Schema Switching (Priority: P1)

When an authenticated user makes an API request, the system resolves their tenant from the JWT, switches the database context to the tenant's dedicated schema, and ensures all queries and writes are scoped to that schema. No data leaks between tenants.

**Why this priority**: Data isolation is the core security promise of multi-tenancy. Without schema switching, all tenants share the same tables, violating the isolation guarantee from Phase 001 (FR-001). Co-equal with US1 — provisioning creates the schema, this story ensures it is actually used.

**Independent Test**: Can be tested by creating two tenants, inserting data in each, and verifying that queries scoped to Tenant A never return Tenant B's data and vice versa.

**Acceptance Scenarios**:

1. **Given** Tenant A ("Cairo Dental") and Tenant B ("Giza Smiles") are both provisioned, **When** a user authenticated under Tenant A creates a patient record, **Then** that patient is stored in Tenant A's schema and is invisible to queries from Tenant B's schema.
2. **Given** a user with a valid JWT containing `tenant_id: <tenant-a-id>`, **When** they make any API request, **Then** the system sets the PostgreSQL `search_path` to the tenant's schema before executing any database operation.
3. **Given** two concurrent requests from different tenants, **When** both execute simultaneously, **Then** each request operates within its own schema with no cross-contamination.
4. **Given** a request has an invalid or missing tenant ID in the JWT, **When** the system attempts to resolve the tenant schema, **Then** the request is rejected with 401 Unauthorized (not silently routed to a default schema).

---

### User Story 3 - Store Cross-Tenant Data in Shared Schema (Priority: P1)

Certain data - specifically tenant/practice records, clinic branch records, dentist profiles, and the clinic-dentist association - is shared across tenants and must remain accessible regardless of which tenant schema is active. This data lives in a dedicated shared schema, queried independently of tenant schema switching.

**Why this priority**: Without shared schema separation, clinic and dentist data would be duplicated across every tenant schema or inaccessible when `search_path` is changed. Cross-tenant entities are a prerequisite for the existing dentist-sharing model (a dentist working at multiple clinics).

**Independent Test**: Can be tested by querying a dentist record while the database context is set to any tenant schema — the dentist must always be accessible.

**Acceptance Scenarios**:

1. **Given** a dentist "Dr. Mona" exists in the shared schema and is associated with both Clinic A and Clinic B, **When** a user in Clinic A queries dentists, **Then** Dr. Mona appears in the results, fetched from the shared schema.
2. **Given** the database context's `search_path` is set to `tenant_abc`, **When** the system queries the `clinics` table, **Then** it reads from the shared schema, not from `tenant_abc`.
3. **Given** a new clinic is onboarded, **When** its record is created, **Then** it is stored in the shared schema alongside all other clinic records.

---

### User Story 4 - Deactivate and Reactivate a Tenant/Practice (Priority: P2)

A platform operator can deactivate a tenant/practice, preventing all users of that tenant from accessing the system. The tenant's data is preserved (not deleted), and the tenant can be reactivated later, restoring full access.

**Why this priority**: Tenant lifecycle management is essential for handling clinics that stop paying, violate terms, or temporarily close. Depends on US1-3 being in place but is not needed for day-to-day clinic operations.

**Independent Test**: Can be tested by deactivating a tenant, attempting to access it as a user of that tenant (expecting rejection), then reactivating and verifying access is restored.

**Acceptance Scenarios**:

1. **Given** a platform operator deactivates Tenant A, **When** a user belonging to Tenant A attempts to make any API request, **Then** the system returns 403 Forbidden with an error indicating the tenant is inactive.
2. **Given** Tenant A is deactivated, **When** the platform operator queries the list of tenants, **Then** Tenant A appears with status `Inactive`, and its schema and data remain intact.
3. **Given** Tenant A was deactivated, **When** the platform operator reactivates it, **Then** users of Tenant A can once again access the system and all their data is intact.
4. **Given** Tenant A is deactivated, **When** a platform operator queries Tenant A's configuration, **Then** the system returns the tenant details including a deactivation timestamp and the operator who deactivated it.

---

### User Story 5 - Complete Clinic Entity with Contact Details (Priority: P2)

The existing Clinic domain entity is extended with branch contact details (phone number, address). These fields are required for tenant onboarding's first clinic branch and for displaying clinic information.

**Why this priority**: The Clinic entity currently only has `Name`, `SchemaName`, and `IsActive`. Phone and address are needed for onboarding (US1) and are part of the originally planned domain model. This completes the entity before downstream features depend on it.

**Independent Test**: Can be tested by creating a clinic with phone and address, then retrieving it and verifying all fields are present.

**Acceptance Scenarios**:

1. **Given** a clinic is onboarded with phone "01112345678" and address "15 Tahrir St, Cairo", **When** the clinic is retrieved, **Then** both phone and address are returned in the response.
2. **Given** a clinic exists, **When** a platform operator updates the clinic's phone number to "01198765432", **Then** the phone number is validated (11-digit Egyptian format) and updated.
3. **Given** a clinic exists, **When** a platform operator updates the address, **Then** the address is persisted and returned on subsequent queries.

---

### Edge Cases

- What happens if schema creation succeeds but migration application fails? → The system rolls back by dropping the newly created schema and deleting the clinic record. The operation is atomic from the caller's perspective.
- What if two onboarding requests arrive simultaneously for the same clinic name? → Clinic names are not unique constraints (two clinics can share a name). Phone number uniqueness prevents true duplicates. Schema names are unique (derived from IDs).
- What happens when a schema migration is added in a future release — are existing tenant schemas updated? → Existing tenant schemas must be migrated during application startup or via a dedicated migration command. The system tracks which migration version each schema is at.
- How does the system handle a corrupted or partially migrated tenant schema? → The system detects schema health during tenant resolution. If a schema is unhealthy, the tenant is flagged and requests are rejected with a clear error until an operator resolves it.
- What if the database runs out of connection pool capacity during concurrent tenant provisioning? → Provisioning operations use a dedicated connection (not from the request pool) and are serialized to prevent overwhelming the database.
- What happens to a deactivated tenant's scheduled appointments and pending invoices? -> Data remains as-is; no cascading cancellations. When reactivated, the tenant resumes with all data intact. Notifications about upcoming appointments for deactivated tenants are suppressed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create a dedicated PostgreSQL schema for each new tenant/practice during onboarding.
- **FR-002**: System MUST apply all current domain model migrations to the newly created tenant schema, ensuring it has the full table structure (patients, appointments, treatment_plans, treatment_items, invoices, invoice_lines, payments).
- **FR-003**: System MUST generate unique, deterministic schema names derived from the tenant's ID (e.g., `tenant_<short_id>`). Schema names MUST be valid PostgreSQL identifiers.
- **FR-004**: System MUST enforce atomicity during onboarding: if schema creation or migration fails, the tenant and clinic records MUST be rolled back - no orphaned records without a corresponding schema.
- **FR-005**: System MUST resolve the authenticated user's tenant from JWT claims and set the PostgreSQL `search_path` to the tenant's schema for all database operations within that request.
- **FR-006**: System MUST store cross-tenant entities (Tenant, Clinic, Dentist, ClinicDentist) in a shared schema accessible regardless of the active tenant schema.
- **FR-007**: System MUST prevent any data access for users belonging to a deactivated tenant, returning an appropriate error.
- **FR-008**: System MUST support tenant deactivation and reactivation by a platform operator without data loss.
- **FR-009**: System MUST validate that the tenant schema exists and is healthy before routing a request to it. Unhealthy or missing schemas MUST result in a clear error.
- **FR-010**: System MUST extend the Clinic entity with phone number (validated Egyptian format, unique) and address fields.
- **FR-011**: System MUST support querying the list of all tenants with their first clinic branch and provisioning status (active, inactive, schema health).
- **FR-012**: System MUST ensure concurrent requests from different tenants are isolated — schema switching for one request MUST NOT affect another.
- **FR-013**: System MUST support applying new migrations to all existing tenant schemas when the application is updated with schema changes.
- **FR-014**: System MUST log all provisioning operations (schema creation, activation, deactivation) for audit purposes.

### Key Entities

- **Tenant**: The isolation boundary entity. Key attributes: practice name, schema name (unique, auto-generated), lifecycle status, provisioning status, schema health, current migration, created date, deactivated date. Stored in the shared schema.
- **Clinic (Branch/Location)**: A branch under a tenant, with contact details. Key attributes: tenant ID, name, phone (unique, validated), address, lifecycle status, created date, deactivated date. Stored in the shared schema.
- **TenantSchema**: A logical concept (not necessarily a separate entity) representing the state of a tenant's PostgreSQL schema. Key attributes: schema name, migration version, health status, last verified date.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new clinic can be onboarded (record created + schema provisioned + migrations applied) in under 10 seconds.
- **SC-002**: Tenant data isolation is verified: queries from Tenant A return zero rows from Tenant B's schema, confirmed by integration tests with at least 2 tenants.
- **SC-003**: Cross-tenant queries (clinics, dentists) return correct results regardless of which tenant schema is currently active.
- **SC-004**: A deactivated tenant's users receive an error within 1 second of attempting any operation, and data is fully intact upon reactivation.
- **SC-005**: Concurrent requests from 10 different tenants execute without any cross-tenant data leakage, verified by a concurrent integration test.
- **SC-006**: Onboarding failure (e.g., simulated migration failure) results in zero orphaned state - no tenant/clinic record without a schema, no schema without a tenant record.
- **SC-007**: All existing tenant schemas are successfully migrated to the latest version when a new migration is applied.

## Assumptions

- The platform operator role is an internal/system role, not a clinic-level role. Clinic onboarding is not a self-service operation in v1 — it is initiated by the platform or by an internal admin tool.
- The existing `ClinicAdmin` role (from Phase 002) is a tenant-level admin, not a platform operator. Platform operator permissions are assumed handled by a super-admin or internal API key mechanism (details deferred to a future phase).
- The `shared` schema holds Tenant, Clinic, Dentist, and ClinicDentist tables. All other operational data lives in tenant-specific schemas.
- The existing `AuthDbContext` continues to operate in the `public` schema for user/auth data. Tenant provisioning does not affect auth data storage.
- Schema names are immutable once assigned. Renaming a schema is not supported.
- The development seed data ("Dev Clinic" with schema `tenant_dev`) will be updated to follow the new provisioning flow, with the dev schema automatically provisioned on startup.
- Subscription/billing for tenants (SaaS billing) is out of scope. Deactivation is an operator action, not an automated billing event.
- The frontend is out of scope. This spec covers the API and infrastructure layers only.

## Review & Acceptance Checklist

- [x] All user stories have clear acceptance scenarios with Given/When/Then
- [x] Priorities are assigned and justified
- [x] Each story is independently testable
- [x] Edge cases are identified and have defined behavior
- [x] Key entities and their relationships are documented
- [x] Multi-tenancy implications are explicitly stated for every story
- [x] Schema isolation mechanics are fully specified
- [x] Success criteria are measurable and technology-agnostic
- [x] Assumptions clearly scope what is and isn't included
- [x] Failure and rollback scenarios are addressed
