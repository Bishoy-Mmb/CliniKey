using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Tenant : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxSchemaNameLength = 63;
    public const int MaxMigrationLength = 150;

    public string Name { get; private set; }
    public string SchemaName { get; private init; }
    public TenantStatus Status { get; private set; }
    public TenantProvisioningStatus ProvisioningStatus { get; private set; }
    public TenantSchemaHealthStatus SchemaHealthStatus { get; private set; }
    public string? CurrentMigration { get; private set; }
    public DateTime? LastSchemaVerifiedAtUtc { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }
    public Guid? DeactivatedByUserId { get; private set; }

    public bool IsActive => Status == TenantStatus.Active;

    private readonly List<Clinic> _clinics = [];
    public IReadOnlyCollection<Clinic> Clinics => _clinics.AsReadOnly();

    private Tenant(Guid id, string name, string schemaName, TimeProvider clock) : base(clock)
    {
        Id = id;
        Name = name;
        SchemaName = schemaName;
        Status = TenantStatus.Active;
        ProvisioningStatus = TenantProvisioningStatus.Pending;
        SchemaHealthStatus = TenantSchemaHealthStatus.Unknown;
    }

    private Tenant()
    {
        Name = null!;
        SchemaName = null!;
    }

    public static Result<Tenant> Create(Guid id, string name, string schemaName, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return Result.Failure<Tenant>(TenantErrors.InvalidName);
        }

        if (!IsValidSchemaName(schemaName))
        {
            return Result.Failure<Tenant>(TenantErrors.InvalidSchemaName);
        }

        var tenant = new Tenant(id, name, schemaName, clock);
        tenant.RaiseDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.SchemaName, tenant.CreatedAtUtc));
        return tenant;
    }

    public Result MarkProvisioning()
    {
        ProvisioningStatus = TenantProvisioningStatus.Provisioning;
        SchemaHealthStatus = TenantSchemaHealthStatus.Unknown;
        MarkUpdated();
        return Result.Success();
    }

    public Result MarkProvisioned(string? currentMigration)
    {
        if (currentMigration is not null && currentMigration.Length > MaxMigrationLength)
        {
            return Result.Failure(TenantErrors.InvalidMigration);
        }

        var now = Clock.GetUtcNow().UtcDateTime;
        ProvisioningStatus = TenantProvisioningStatus.Provisioned;
        SchemaHealthStatus = TenantSchemaHealthStatus.Healthy;
        CurrentMigration = currentMigration;
        LastSchemaVerifiedAtUtc = now;
        DeactivatedAtUtc = null;
        DeactivatedByUserId = null;
        MarkUpdated();
        RaiseDomainEvent(new TenantProvisionedEvent(Id, SchemaName, CurrentMigration, now));
        return Result.Success();
    }

    public Result MarkProvisioningFailed()
    {
        ProvisioningStatus = TenantProvisioningStatus.Failed;
        SchemaHealthStatus = TenantSchemaHealthStatus.Unhealthy;
        MarkUpdated();
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == TenantStatus.Active) return Result.Failure(TenantErrors.AlreadyActive);
        if (SchemaHealthStatus != TenantSchemaHealthStatus.Healthy) return Result.Failure(TenantErrors.SchemaUnhealthy);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = TenantStatus.Active;
        DeactivatedAtUtc = null;
        DeactivatedByUserId = null;
        MarkUpdated();
        RaiseDomainEvent(new TenantActivatedEvent(Id, now));
        return Result.Success();
    }

    public Result Deactivate(Guid? operatorUserId = null)
    {
        if (Status == TenantStatus.Inactive) return Result.Failure(TenantErrors.AlreadyInactive);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = TenantStatus.Inactive;
        DeactivatedAtUtc = now;
        DeactivatedByUserId = operatorUserId;
        MarkUpdated();
        RaiseDomainEvent(new TenantDeactivatedEvent(Id, operatorUserId, now));
        return Result.Success();
    }

    public Result Suspend(Guid? operatorUserId)
    {
        if (Status == TenantStatus.Suspended) return Result.Failure(TenantErrors.AlreadySuspended);

        var now = Clock.GetUtcNow().UtcDateTime;
        Status = TenantStatus.Suspended;
        DeactivatedAtUtc = now;
        DeactivatedByUserId = operatorUserId;
        MarkUpdated();
        RaiseDomainEvent(new TenantDeactivatedEvent(Id, operatorUserId, now));
        return Result.Success();
    }

    public Result MarkSchemaHealth(
        TenantSchemaHealthStatus status,
        string? currentMigration,
        DateTime verifiedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            return Result.Failure(TenantErrors.InvalidSchemaHealth);
        }

        if (currentMigration is not null && currentMigration.Length > MaxMigrationLength)
        {
            return Result.Failure(TenantErrors.InvalidMigration);
        }

        SchemaHealthStatus = status;
        CurrentMigration = currentMigration;
        LastSchemaVerifiedAtUtc = verifiedAtUtc;
        MarkUpdated();
        return Result.Success();
    }

    public Result AddClinic(Clinic clinic)
    {
        if (clinic.TenantId != Id)
        {
            return Result.Failure(TenantErrors.ClinicTenantMismatch);
        }

        if (_clinics.Any(c => c.Id == clinic.Id))
        {
            return Result.Success();
        }

        _clinics.Add(clinic);
        MarkUpdated();
        return Result.Success();
    }

    private static bool IsValidSchemaName(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName) || schemaName.Length > MaxSchemaNameLength)
        {
            return false;
        }

        return schemaName.All(c => char.IsAsciiLetterLower(c) || char.IsDigit(c) || c == '_')
            && char.IsAsciiLetter(schemaName[0]);
    }
}
