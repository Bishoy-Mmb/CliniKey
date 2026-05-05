using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.ValueObjects;

public sealed class ToothCode : ValueObject
{
    public int Value { get; }

    private ToothCode(int value)
    {
        Value = value;
    }

    public static Result<ToothCode> Create(int value)
    {
        if (!IsValidToothCode(value))
        {
            return Result.Failure<ToothCode>(Error.Validation("ToothCode.Invalid", "Invalid FDI tooth code."));
        }

        return new ToothCode(value);
    }

    private static bool IsValidToothCode(int value)
    {
        // Permanent teeth
        if ((value >= 11 && value <= 18) ||
            (value >= 21 && value <= 28) ||
            (value >= 31 && value <= 38) ||
            (value >= 41 && value <= 48))
        {
            return true;
        }

        // Deciduous teeth
        if ((value >= 51 && value <= 55) ||
            (value >= 61 && value <= 65) ||
            (value >= 71 && value <= 75) ||
            (value >= 81 && value <= 85))
        {
            return true;
        }

        return false;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}
