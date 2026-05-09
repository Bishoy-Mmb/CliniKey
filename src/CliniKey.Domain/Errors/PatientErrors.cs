using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class PatientErrors
{
    public static readonly Error DuplicatePhone = Error.Conflict(
        "Patient.DuplicatePhone",
        "A patient with this phone number already exists.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Patient.NotFound",
        $"The patient with ID {id} was not found.");
}
