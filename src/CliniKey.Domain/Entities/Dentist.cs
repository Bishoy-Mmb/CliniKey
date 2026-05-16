using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Dentist : AggregateRoot<Guid>, IAuditableEntity
{
    public string FullName { get; private set; }
    public string Specialization { get; private set; }
    public string LicenseNumber { get; private set; }

    private Dentist(Guid id, string fullName, string specialization, string licenseNumber)
    {
        Id = id;
        FullName = fullName;
        Specialization = specialization;
        LicenseNumber = licenseNumber;
    }

    private Dentist() { FullName = null!; Specialization = null!; LicenseNumber = null!; }

    public static Dentist Create(string fullName, string specialization, string licenseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(specialization);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);

        return new Dentist(Guid.NewGuid(), fullName, specialization, licenseNumber);
    }

    public Result UpdateSpecialization(string specialization)
    {
        if (string.IsNullOrWhiteSpace(specialization))
        {
            return Result.Failure(DentistErrors.InvalidSpecialization);
        }

        if (Specialization == specialization) return Result.Success();

        Specialization = specialization;
        MarkUpdated();
        return Result.Success();
    }

    public Result UpdateLicenseNumber(string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return Result.Failure(DentistErrors.InvalidLicenseNumber);
        }

        if (LicenseNumber == licenseNumber) return Result.Success();

        LicenseNumber = licenseNumber;
        MarkUpdated();
        return Result.Success();
    }
}
