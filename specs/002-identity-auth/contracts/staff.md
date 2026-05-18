# API Contracts: Staff Invitation

**Feature**: 002-identity-auth  
**Base Path**: `/api/v1/staff`

---

## POST /api/v1/staff/invite

Invite a new staff member (Dentist or Receptionist) to the current tenant.

**Authorization**: `[Authorize(Roles = "ClinicAdmin")]`

**Request**:
```json
{
  "email": "dentist@clinic.com",
  "password": "Temp@2026!",
  "fullName": "Dr. Mona Ali",
  "role": "Dentist",
  "specialization": "Orthodontics",
  "licenseNumber": "LIC-EG-2026-001"
}
```

**Field rules**:
- `email`: Required, valid email format, max 256 chars
- `password`: Required, same complexity rules as registration
- `fullName`: Required, max 200 chars
- `role`: Required, must be `Dentist` or `Receptionist` (ClinicAdmin cannot invite other ClinicAdmins)
- `specialization`: Required if `role == Dentist`, ignored otherwise
- `licenseNumber`: Required if `role == Dentist`, ignored otherwise

**Success Response** — `201 Created`:
```json
{
  "userId": "guid",
  "email": "dentist@clinic.com",
  "role": "Dentist",
  "dentistId": "guid"
}
```

**Side effects**:
- Creates an `ApplicationUser` in the `public` schema with the specified role and the admin's `TenantId`
- If role is `Dentist`: creates a `Dentist` entity, a `ClinicDentist` link, and sets `ApplicationUser.DentistId`
- If role is `Receptionist`: only creates the user account

**Error Responses**:
| Status | Error Code | Condition |
|--------|-----------|-----------|
| 400 | `Validation.*` | Invalid input |
| 403 | — | Caller is not `ClinicAdmin` |
| 409 | `Auth.DuplicateEmail` | Email already registered |
| 400 | `Auth.InvalidRole` | Role is not `Dentist` or `Receptionist` |

---

## Existing Endpoint Authorization Changes

The following existing endpoints gain `[Authorize]` attributes in this phase:

| Controller | Endpoint | Allowed Roles |
|-----------|----------|---------------|
| `PatientsController` | All | `ClinicAdmin`, `Dentist`, `Receptionist` |
| `AppointmentsController` | All | `ClinicAdmin`, `Dentist`, `Receptionist` |
| `TreatmentPlansController` | All | `ClinicAdmin`, `Dentist` |
| `InvoicesController` | All | `ClinicAdmin`, `Receptionist` |
