using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class AppointmentErrors
{
    public static readonly Error TimeConflict = Error.Conflict(
        "Appointment.TimeConflict",
        "The selected time slot conflicts with another appointment.");

    public static readonly Error InvalidTransition = Error.Validation(
        "Appointment.InvalidTransition",
        "The requested status transition is not allowed from the current state.");

    public static readonly Error NotFound = new(
        "Appointment.NotFound",
        "The appointment with the specified identifier was not found.",
        ErrorType.NotFound);

    public static readonly Error PastDate = Error.Validation(
        "Appointment.PastDate",
        "Appointments cannot be scheduled in the past.");
}
