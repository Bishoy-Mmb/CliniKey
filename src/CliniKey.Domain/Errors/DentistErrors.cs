using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class DentistErrors
{
    public static readonly Error InvalidSpecialization = Error.Validation(
        "Dentist.InvalidSpecialization",
        "Specialization cannot be empty.");

    public static readonly Error InvalidLicenseNumber = Error.Validation(
        "Dentist.InvalidLicenseNumber",
        "License number cannot be empty.");
}
