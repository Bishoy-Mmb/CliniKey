using CliniKey.SharedKernel.Primitives;
using System.Text.RegularExpressions;

namespace CliniKey.Domain.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    private const string Pattern = @"^01[0125][0-9]{8}$";

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PhoneNumber>(Error.Validation("PhoneNumber.Empty", "Phone number is required."));
        }

        if (!EgyptianMobileRegex().IsMatch(value))
        {
            return Result.Failure<PhoneNumber>(Error.Validation("PhoneNumber.InvalidFormat", "Phone number must be a valid Egyptian mobile number (11 digits, starting with 010, 011, 012, or 015)."));
        }

        return new PhoneNumber(value);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(Pattern)]
    private static partial Regex EgyptianMobileRegex();
}
