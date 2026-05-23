# Contract: Clinic Tenant Management

**Base route**: `/api/v1/tenants/clinics`  
**Authorization**: `Policies.CanManageTenants` platform-operator policy  
**Tenant middleware**: These endpoints are platform-scoped and must not require a tenant schema context.

---

## POST `/api/v1/tenants/clinics`

Onboard a new clinic and provision its tenant schema.

### Request

```json
{
  "name": "Cairo Dental Center",
  "phone": "01112345678",
  "address": "15 Tahrir St, Cairo"
}
```

### Response `201 Created`

```json
{
  "clinicId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
  "name": "Cairo Dental Center",
  "phone": "01112345678",
  "address": "15 Tahrir St, Cairo",
  "schemaName": "tenant_3f0d1b0b",
  "status": "Active",
  "provisioningStatus": "Provisioned",
  "schemaHealthStatus": "Healthy",
  "currentMigration": "202605230001_InitialTenantOperationalSchema",
  "createdAtUtc": "2026-05-23T09:15:00Z"
}
```

### Errors

| Status | Error code | Meaning |
|--------|------------|---------|
| 400 | `Clinic.InvalidName` | Name is missing or too long |
| 400 | `PhoneNumber.InvalidFormat` | Phone is not a valid Egyptian mobile number |
| 400 | `Clinic.InvalidAddress` | Address is missing or too long |
| 409 | `Tenant.DuplicatePhone` | Another clinic already uses the phone |
| 500 | `Tenant.ProvisioningFailed` | Schema creation or migration failed and rollback completed |

---

## GET `/api/v1/tenants/clinics`

List clinics and tenant health.

### Query Parameters

| Name | Type | Required | Notes |
|------|------|----------|-------|
| `status` | string | No | `Active`, `Inactive`, `Suspended` |
| `health` | string | No | `Healthy`, `Missing`, `MigrationPending`, `Unhealthy` |
| `page` | int | No | Default `1` |
| `pageSize` | int | No | Default `50`, max `100` |

### Response `200 OK`

```json
{
  "items": [
    {
      "clinicId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
      "name": "Cairo Dental Center",
      "phone": "01112345678",
      "address": "15 Tahrir St, Cairo",
      "schemaName": "tenant_3f0d1b0b",
      "status": "Active",
      "provisioningStatus": "Provisioned",
      "schemaHealthStatus": "Healthy",
      "lastSchemaVerifiedAtUtc": "2026-05-23T09:15:00Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 1
}
```

---

## GET `/api/v1/tenants/clinics/{clinicId}`

Return clinic details, contact information, lifecycle status, and schema health.

### Response `200 OK`

```json
{
  "clinicId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
  "name": "Cairo Dental Center",
  "phone": "01112345678",
  "address": "15 Tahrir St, Cairo",
  "schemaName": "tenant_3f0d1b0b",
  "status": "Inactive",
  "provisioningStatus": "Provisioned",
  "schemaHealthStatus": "Healthy",
  "currentMigration": "202605230001_InitialTenantOperationalSchema",
  "deactivatedAtUtc": "2026-05-23T10:00:00Z",
  "deactivatedByUserId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "createdAtUtc": "2026-05-23T09:15:00Z",
  "updatedAtUtc": "2026-05-23T10:00:00Z"
}
```

### Errors

| Status | Error code |
|--------|------------|
| 404 | `Clinic.NotFound` |

---

## PUT `/api/v1/tenants/clinics/{clinicId}/contact`

Update clinic phone and address.

### Request

```json
{
  "phone": "01198765432",
  "address": "22 Nile Corniche, Cairo"
}
```

### Response `204 No Content`

### Errors

| Status | Error code | Meaning |
|--------|------------|---------|
| 400 | `PhoneNumber.InvalidFormat` | Invalid Egyptian mobile number |
| 400 | `Clinic.InvalidAddress` | Address is missing or too long |
| 404 | `Clinic.NotFound` | Clinic does not exist |
| 409 | `Tenant.DuplicatePhone` | Another clinic already uses the phone |

---

## POST `/api/v1/tenants/clinics/{clinicId}/deactivate`

Deactivate a clinic without deleting schema data.

### Request

```json
{
  "reason": "Temporary closure"
}
```

### Response `204 No Content`

### Errors

| Status | Error code |
|--------|------------|
| 404 | `Clinic.NotFound` |
| 409 | `Clinic.AlreadyInactive` |

---

## POST `/api/v1/tenants/clinics/{clinicId}/activate`

Reactivate a clinic after verifying that the schema exists and is healthy.

### Response `204 No Content`

### Errors

| Status | Error code | Meaning |
|--------|------------|---------|
| 404 | `Clinic.NotFound` | Clinic does not exist |
| 409 | `Clinic.AlreadyActive` | Clinic is already active |
| 409 | `Tenant.SchemaUnhealthy` | Schema is missing or unhealthy |

