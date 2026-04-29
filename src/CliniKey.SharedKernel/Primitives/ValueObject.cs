namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Base class for value objects. Equality is based on structural comparison
/// of all atomic values rather than identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetAtomicValues();

    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
            return false;

        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public override bool Equals(object? obj) =>
        obj is ValueObject valueObject && Equals(valueObject);

    public override int GetHashCode() =>
        GetAtomicValues()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !Equals(left, right);
}
