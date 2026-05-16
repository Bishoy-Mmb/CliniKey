using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Clinic : AggregateRoot<Guid>, IAuditableEntity
{
    public string Name { get; private set; }
    public string SchemaName { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<ClinicDentist> _clinicDentists = [];
    public IReadOnlyCollection<ClinicDentist> ClinicDentists => _clinicDentists.AsReadOnly();

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

    public Result Activate()
    {
        if (IsActive) return Result.Success();
        IsActive = true;
        MarkUpdated();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Success();
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
