using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class TreatmentPlanErrors
{
    public static readonly Error EmptyPlan = Error.Validation(
        "TreatmentPlan.EmptyPlan",
        "A treatment plan must contain at least one treatment item.");

    public static readonly Error InvalidToothCode = Error.Validation(
        "TreatmentPlan.InvalidToothCode",
        "The specified tooth code is invalid.");

    public static readonly Error InvalidTransition = Error.Validation(
        "TreatmentPlan.InvalidTransition",
        "The requested state transition is invalid for the current status.");

    public static readonly Error MixedCurrencies = Error.Validation(
        "TreatmentPlan.MixedCurrencies",
        "All treatment items must use the same currency.");

    public static Error NotFound(Guid id) => Error.NotFound("TreatmentPlan", id);
}
