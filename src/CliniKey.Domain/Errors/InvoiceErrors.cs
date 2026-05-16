using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Errors;

public static class InvoiceErrors
{
    public static readonly Error AlreadyPaid = Error.Conflict(
        "Invoice.AlreadyPaid",
        "The invoice is already fully paid.");

    public static readonly Error Overpayment = Error.Validation(
        "Invoice.Overpayment",
        "The payment amount exceeds the remaining balance.");

    public static readonly Error CannotVoid = Error.Validation(
        "Invoice.CannotVoid",
        "The invoice cannot be voided because it is partially or fully paid.");

    public static readonly Error InvalidTransition = Error.Validation(
        "Invoice.InvalidTransition",
        "The requested state transition is invalid for the current status.");

    public static Error NotFound(Guid id) => Error.NotFound("Invoice", id);
}
