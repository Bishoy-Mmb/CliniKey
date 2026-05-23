using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class ClinicErrors
{
    public static readonly Error AlreadyActive = Error.Conflict(
        "Clinic.AlreadyActive",
        "Clinic is already active.");

    public static readonly Error AlreadyInactive = Error.Conflict(
        "Clinic.AlreadyInactive",
        "Clinic is already inactive.");

    public static readonly Error InvalidName = Error.Validation(
        "Clinic.InvalidName",
        "Clinic name is invalid.");

    public static readonly Error InvalidSchemaName = Error.Validation(
        "Clinic.InvalidSchemaName",
        "Clinic schema name is invalid.");

    public static readonly Error DentistAlreadyAdded = Error.Conflict(
        "Clinic.DentistAlreadyAdded",
        "Dentist is already added to this clinic.");

    public static readonly Error DentistNotFound = Error.Validation(
        "Clinic.DentistNotFound",
        "Dentist is not associated with this clinic.");
}
