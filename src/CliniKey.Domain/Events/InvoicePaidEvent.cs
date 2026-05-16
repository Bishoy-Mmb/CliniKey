using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Events;

public sealed record InvoicePaidEvent(Guid InvoiceId, DateTime OccurredOnUtc) : IDomainEvent;
