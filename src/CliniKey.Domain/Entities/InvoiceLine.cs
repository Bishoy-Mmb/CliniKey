using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class InvoiceLine : Entity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; }
    public Money Amount { get; private set; }
    public decimal VatRate { get; private set; }

    public Result<Money> CalculateVatAmount()
    {
        var vatValue = Amount.Amount * VatRate;
        return Money.Create(vatValue, Amount.Currency);
    }

    private InvoiceLine()
    {
        Description = null!;
        Amount = null!;
    }

    internal InvoiceLine(string description, Money amount, decimal vatRate)
    {
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        VatRate = vatRate;
    }
}
