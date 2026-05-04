namespace CliniKey.SharedKernel.Interfaces;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    DateTime? UpdatedAtUtc { get; }
}
