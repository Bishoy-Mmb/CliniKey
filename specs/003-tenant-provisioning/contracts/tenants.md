# Contract: Tenant Management

**Base route**: `/api/v1/tenants`  
**Authorization**: `Policies.CanManageTenants` platform-operator policy  
**Tenant middleware**: These endpoints are platform/control-plane endpoints and do not require a tenant schema context.

---

## POST `/api/v1/tenants`

Onboard a new tenant/practice and create its first clinic branch.

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
  "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
  "clinicId": "7a4a15b8-2456-4697-901a-0bd67c3b5e72",
  "name": "Cairo Dental Center",
  "phone": "01112345678",
  "address": "15 Tahrir St, Cairo",
  "schemaName": "tenant_3f0d1b0b4e0f4bcb9fb963f03e432c0f",
  "status": "Active",
  "tenantStatus": "Active",
  "provisioningStatus": "Provisioned",
  "schemaHealthStatus": "Healthy",
  "currentMigration": "202605230001_InitialTenantOperationalSchema",
  "createdAtUtc": "2026-05-23T09:15:00Z"
}
```

`status` is the first clinic branch status. `tenantStatus` is the practice lifecycle status.

### Errors

| Status | Error code | Meaning |
| --- | --- | --- |
| 400 | `Tenant.InvalidName` | Tenant/practice name is missing or too long |
| 400 | `PhoneNumber.InvalidFormat` | Phone is not a valid Egyptian mobile number |
| 400 | `Clinic.InvalidAddress` | First branch address is missing or too long |
| 409 | `Tenant.DuplicatePhone` | Another clinic branch already uses the phone |
| 500 | `Tenant.ProvisioningFailed` | Schema creation or migration failed and rollback completed |

---

## GET `/api/v1/tenants`

List tenants/practices with their first clinic branch and schema health.

### Query Parameters

| Name | Type | Required | Notes |
| --- | --- | --- | --- |
| `status` | string | No | Tenant status: `Active`, `Inactive`, `Suspended` |
| `health` | string | No | `Healthy`, `Missing`, `MigrationPending`, `Unhealthy` |
| `page` | int | No | Default `1` |
| `pageSize` | int | No | Default `50`, max `100` |

### Response `200 OK`

```json
{
  "items": [
    {
      "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
      "clinicId": "7a4a15b8-2456-4697-901a-0bd67c3b5e72",
      "name": "Cairo Dental Center",
      "phone": "01112345678",
      "address": "15 Tahrir St, Cairo",
      "schemaName": "tenant_3f0d1b0b4e0f4bcb9fb963f03e432c0f",
      "status": "Active",
      "tenantStatus": "Active",
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

## GET `/api/v1/tenants/{tenantId}`

Return tenant/practice details plus the first clinic branch contact information.

### Response `200 OK`

```json
{
  "tenantId": "3f0d1b0b-4e0f-4bcb-9fb9-63f03e432c0f",
  "clinicId": "7a4a15b8-2456-4697-901a-0bd67c3b5e72",
  "name": "Cairo Dental Center",
  "phone": "01112345678",
  "address": "15 Tahrir St, Cairo",
  "schemaName": "tenant_3f0d1b0b4e0f4bcb9fb963f03e432c0f",
  "status": "Active",
  "tenantStatus": "Inactive",
  "provisioningStatus": "Provisioned",
  "schemaHealthStatus": "Healthy",
  "currentMigration": "202605230001_InitialTenantOperationalSchema",
  "lastSchemaVerifiedAtUtc": "2026-05-23T09:15:00Z",
  "tenantDeactivatedAtUtc": "2026-05-23T10:00:00Z",
  "tenantDeactivatedByUserId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "deactivatedAtUtc": null,
  "deactivatedByUserId": null,
  "createdAtUtc": "2026-05-23T09:15:00Z",
  "updatedAtUtc": "2026-05-23T10:00:00Z"
}
```

### Errors

| Status | Error code |
| --- | --- |
| 404 | `Clinic.NotFound` |
| 401 | `Tenant.NotFound` |

---

## PUT `/api/v1/tenants/{tenantId}/clinics/{clinicId}/contact`

Update a clinic branch phone and address under the tenant.

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
| --- | --- | --- |
| 400 | `PhoneNumber.InvalidFormat` | Invalid Egyptian mobile number |
| 400 | `Clinic.InvalidAddress` | Address is missing or too long |
| 400 | `Tenant.ClinicTenantMismatch` | Clinic branch does not belong to the tenant route |
| 404 | `Clinic.NotFound` | Clinic branch does not exist |
| 409 | `Tenant.DuplicatePhone` | Another clinic branch already uses the phone |

---

## POST `/api/v1/tenants/{tenantId}/deactivate`

Deactivate a tenant/practice without deleting schema data.

### Request

```json
{
  "reason": "Temporary closure"
}
```

### Response `204 No Content`

### Errors

| Status | Error code |
| --- | --- |
| 401 | `Tenant.NotFound` |
| 409 | `Tenant.AlreadyInactive` |

---

## POST `/api/v1/tenants/{tenantId}/activate`

Reactivate a tenant/practice after verifying that the tenant schema is healthy.

### Response `204 No Content`

### Errors

| Status | Error code | Meaning |
| --- | --- | --- |
| 401 | `Tenant.NotFound` | Tenant does not exist |
| 409 | `Tenant.AlreadyActive` | Tenant is already active |
| 409 | `Tenant.SchemaUnhealthy` | Schema is missing or unhealthy |
