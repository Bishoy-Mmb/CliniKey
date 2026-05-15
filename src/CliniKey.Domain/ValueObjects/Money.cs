using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static readonly Money Zero = new(0m, "EGP");

    public static Result<Money> Create(decimal amount, string currency = "EGP")
    {
        if (amount < 0)
        {
            return Result.Failure<Money>(Error.Validation("Money.NegativeAmount", "Amount cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(Error.Validation("Money.InvalidCurrency", "Currency is required."));
        }

        return new Money(amount, currency);
    }

    public Result<Money> Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<Money>(Error.Validation(
                "Money.CurrencyMismatch",
                $"Cannot add {other.Currency} to {Currency}. All amounts must use the same currency."));
        }

        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
}
