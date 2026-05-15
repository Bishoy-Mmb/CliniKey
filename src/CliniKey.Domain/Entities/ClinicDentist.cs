using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class ClinicDentist : Entity<Guid>
{
    public Guid ClinicId { get; private set; }
    public Guid DentistId { get; private set; }

    private ClinicDentist(Guid id, Guid clinicId, Guid dentistId)
    {
        Id = id;
        ClinicId = clinicId;
        DentistId = dentistId;
    }

    private ClinicDentist() { }

    public static ClinicDentist Create(Guid clinicId, Guid dentistId)
    {
        return new ClinicDentist(Guid.NewGuid(), clinicId, dentistId);
    }
}
