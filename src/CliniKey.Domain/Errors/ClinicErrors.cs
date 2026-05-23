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

    public static readonly Error AlreadySuspended = Error.Conflict(
        "Clinic.AlreadySuspended",
        "Clinic is already suspended.");

    public static readonly Error InvalidName = Error.Validation(
        "Clinic.InvalidName",
        "Clinic name is invalid.");

    public static readonly Error InvalidAddress = Error.Validation(
        "Clinic.InvalidAddress",
        "Clinic address is invalid.");

    public static readonly Error InvalidSchemaName = Error.Validation(
        "Clinic.InvalidSchemaName",
        "Clinic schema name is invalid.");

    public static readonly Error InvalidMigration = Error.Validation(
        "Clinic.InvalidMigration",
        "Clinic migration value is invalid.");

    public static readonly Error InvalidSchemaHealth = Error.Validation(
        "Clinic.InvalidSchemaHealth",
        "Clinic schema health status is invalid.");

    public static readonly Error NotFound = Error.NotFound("Clinic", "requested");

    public static readonly Error DentistAlreadyAdded = Error.Conflict(
        "Clinic.DentistAlreadyAdded",
        "Dentist is already added to this clinic.");

    public static readonly Error DentistNotFound = Error.Validation(
        "Clinic.DentistNotFound",
        "Dentist is not associated with this clinic.");
}
