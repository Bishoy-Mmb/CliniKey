using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.ValueObjects;

public sealed class LocalizedString : ValueObject
{
    public string En { get; }
    public string? Ar { get; }

    private LocalizedString(string en, string? ar)
    {
        En = en;
        Ar = ar;
    }

    public static Result<LocalizedString> Create(string en, string? ar = null)
    {
        if (string.IsNullOrWhiteSpace(en))
        {
            return Result.Failure<LocalizedString>(new Error("LocalizedString.EmptyEnglish", "English value is required."));
        }

        return new LocalizedString(en, string.IsNullOrWhiteSpace(ar) ? null : ar);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return En;
        if (Ar is not null)
        {
            yield return Ar;
        }
    }
}
