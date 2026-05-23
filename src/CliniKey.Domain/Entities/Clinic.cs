using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Clinic : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxSchemaNameLength = 63;

    public string Name { get; private set; }
    public string SchemaName { get; private init; }
    public bool IsActive { get; private set; }

    private readonly List<ClinicDentist> _clinicDentists = [];
    public IReadOnlyCollection<ClinicDentist> ClinicDentists => _clinicDentists.AsReadOnly();

    private Clinic(Guid id, string name, string schemaName, bool isActive, TimeProvider clock)
        : base(clock)
    {
        Id = id;
        Name = name;
        SchemaName = schemaName;
        IsActive = isActive;
    }

    private Clinic() { Name = null!; SchemaName = null!; }

    public static Result<Clinic> Create(string name, string schemaName, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return Result.Failure<Clinic>(ClinicErrors.InvalidName);
        }

        if (string.IsNullOrWhiteSpace(schemaName) || schemaName.Length > MaxSchemaNameLength)
        {
            return Result.Failure<Clinic>(ClinicErrors.InvalidSchemaName);
        }

        return new Clinic(Guid.NewGuid(), name, schemaName, true, clock);
    }

    public Result Activate()
    {
        if (IsActive) return Result.Failure(ClinicErrors.AlreadyActive);

        IsActive = true;
        MarkUpdated();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Failure(ClinicErrors.AlreadyInactive);

        IsActive = false;
        MarkUpdated();
        return Result.Success();
    }

    public Result AddDentist(Guid dentistId)
    {
        if (_clinicDentists.Any(cd => cd.DentistId == dentistId))
        {
            return Result.Failure(ClinicErrors.DentistAlreadyAdded);
        }

        _clinicDentists.Add(ClinicDentist.Create(Id, dentistId));
        MarkUpdated();
        return Result.Success();
    }

    public Result RemoveDentist(Guid dentistId)
    {
        var clinicDentist = _clinicDentists.FirstOrDefault(cd => cd.DentistId == dentistId);
        if (clinicDentist is null)
        {
            return Result.Failure(ClinicErrors.DentistNotFound);
        }

        _clinicDentists.Remove(clinicDentist);
        MarkUpdated();
        return Result.Success();
    }
}
