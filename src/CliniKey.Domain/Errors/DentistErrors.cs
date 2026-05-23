using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class DentistErrors
{
    public static readonly Error InvalidFullName = Error.Validation(
        "Dentist.InvalidFullName",
        "Full name cannot be empty.");

    public static readonly Error FullNameTooLong = Error.Validation(
        "Dentist.FullNameTooLong",
        "Full name must not exceed 200 characters.");

    public static readonly Error InvalidSpecialization = Error.Validation(
        "Dentist.InvalidSpecialization",
        "Specialization cannot be empty.");

    public static readonly Error SpecializationTooLong = Error.Validation(
        "Dentist.SpecializationTooLong",
        "Specialization must not exceed 100 characters.");

    public static readonly Error InvalidLicenseNumber = Error.Validation(
        "Dentist.InvalidLicenseNumber",
        "License number cannot be empty.");

    public static readonly Error LicenseNumberTooLong = Error.Validation(
        "Dentist.LicenseNumberTooLong",
        "License number must not exceed 50 characters.");
}
