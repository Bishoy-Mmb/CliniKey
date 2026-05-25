using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class TenantProvisioningAuditLog : Entity<Guid>
{
    public const int MaxSchemaNameLength = 63;
    public const int MaxOperationLength = 100;
    public const int MaxStatusLength = 50;
    public const int MaxMessageLength = 1000;

    public Guid? TenantId { get; private set; }
    public string? SchemaName { get; private set; }
    public string Operation { get; private set; }
    public string Status { get; private set; }
    public string? Message { get; private set; }
    public Guid? OperatorUserId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private TenantProvisioningAuditLog(
        Guid id,
        Guid? tenantId,
        string? schemaName,
        string operation,
        string status,
        string? message,
        Guid? operatorUserId,
        DateTime occurredAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        SchemaName = schemaName;
        Operation = operation;
        Status = status;
        Message = message;
        OperatorUserId = operatorUserId;
        OccurredAtUtc = occurredAtUtc;
    }

    private TenantProvisioningAuditLog()
    {
        Operation = null!;
        Status = null!;
    }

    public static TenantProvisioningAuditLog Create(
        Guid? tenantId,
        string? schemaName,
        string operation,
        string status,
        string? message,
        Guid? operatorUserId,
        TimeProvider clock)
    {
        return new TenantProvisioningAuditLog(
            Guid.NewGuid(),
            tenantId,
            schemaName,
            operation,
            status,
            message,
            operatorUserId,
            clock.GetUtcNow().UtcDateTime);
    }
}
