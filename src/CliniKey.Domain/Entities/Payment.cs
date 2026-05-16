using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Payment : Entity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string? ReferenceNumber { get; private set; }

    private Payment()
    {
        Amount = null!;
    }

    internal Payment(Money amount, PaymentMethod method, DateTime paidAtUtc, string? referenceNumber)
    {
        Id = Guid.NewGuid();
        Amount = amount;
        Method = method;
        PaidAtUtc = paidAtUtc;
        ReferenceNumber = referenceNumber;
    }
}
