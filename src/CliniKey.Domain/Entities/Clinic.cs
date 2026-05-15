using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Clinic : AggregateRoot<Guid>, IAuditableEntity
{
    public string Name { get; private set; }
    public string SchemaName { get; private set; }
    public bool IsActive { get; private set; }

    private Clinic(Guid id, string name, string schemaName, bool isActive)
    {
        Id = id;
        Name = name;
        SchemaName = schemaName;
        IsActive = isActive;
    }

    private Clinic() { Name = null!; SchemaName = null!; }

    public static Clinic Create(string name, string schemaName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        return new Clinic(Guid.NewGuid(), name, schemaName, true);
    }
}
