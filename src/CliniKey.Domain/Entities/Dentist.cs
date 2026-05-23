using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Dentist : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxFullNameLength = 200;
    public const int MaxSpecializationLength = 100;
    public const int MaxLicenseNumberLength = 50;

    public string FullName { get; private set; }
    public string Specialization { get; private set; }
    public string LicenseNumber { get; private set; }

    private Dentist(
        Guid id,
        string fullName,
        string specialization,
        string licenseNumber,
        TimeProvider clock) : base(clock)
    {
        Id = id;
        FullName = fullName;
        Specialization = specialization;
        LicenseNumber = licenseNumber;
    }

    private Dentist() { FullName = null!; Specialization = null!; LicenseNumber = null!; }

    public static Result<Dentist> Create(
        string fullName,
        string specialization,
        string licenseNumber,
        TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<Dentist>(DentistErrors.InvalidFullName);
        }

        if (fullName.Length > MaxFullNameLength)
        {
            return Result.Failure<Dentist>(DentistErrors.FullNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(specialization))
        {
            return Result.Failure<Dentist>(DentistErrors.InvalidSpecialization);
        }

        if (specialization.Length > MaxSpecializationLength)
        {
            return Result.Failure<Dentist>(DentistErrors.SpecializationTooLong);
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return Result.Failure<Dentist>(DentistErrors.InvalidLicenseNumber);
        }

        if (licenseNumber.Length > MaxLicenseNumberLength)
        {
            return Result.Failure<Dentist>(DentistErrors.LicenseNumberTooLong);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var dentist = new Dentist(Guid.NewGuid(), fullName, specialization, licenseNumber, clock);
        dentist.RaiseDomainEvent(new DentistCreatedEvent(dentist.Id, now));

        return dentist;
    }

    public Result UpdateFullName(string fullName)
    {
        if (FullName == fullName) return Result.Success();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(DentistErrors.InvalidFullName);
        }

        if (fullName.Length > MaxFullNameLength)
        {
            return Result.Failure(DentistErrors.FullNameTooLong);
        }

        FullName = fullName;
        MarkUpdated();
        return Result.Success();
    }

    public Result UpdateSpecialization(string specialization)
    {
        if (Specialization == specialization) return Result.Success();

        if (string.IsNullOrWhiteSpace(specialization))
        {
            return Result.Failure(DentistErrors.InvalidSpecialization);
        }

        if (specialization.Length > MaxSpecializationLength)
        {
            return Result.Failure(DentistErrors.SpecializationTooLong);
        }

        Specialization = specialization;
        MarkUpdated();
        return Result.Success();
    }

    public Result UpdateLicenseNumber(string licenseNumber)
    {
        if (LicenseNumber == licenseNumber) return Result.Success();

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return Result.Failure(DentistErrors.InvalidLicenseNumber);
        }

        if (licenseNumber.Length > MaxLicenseNumberLength)
        {
            return Result.Failure(DentistErrors.LicenseNumberTooLong);
        }

        LicenseNumber = licenseNumber;
        MarkUpdated();
        return Result.Success();
    }
}
