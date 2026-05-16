using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class ClinicErrors
{
    public static readonly Error DentistAlreadyAdded = Error.Conflict(
        "Clinic.DentistAlreadyAdded",
        "Dentist is already added to this clinic.");

    public static readonly Error DentistNotFound = Error.Validation(
        "Clinic.DentistNotFound",
        "Dentist is not associated with this clinic.");
}
