using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.ValueObjects;

public sealed class PatientName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }

    private PatientName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static Result<PatientName> Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
        {
            return Result.Failure<PatientName>(new Error("PatientName.InvalidFirstName", "First name is required and cannot exceed 100 characters."));
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
        {
            return Result.Failure<PatientName>(new Error("PatientName.InvalidLastName", "Last name is required and cannot exceed 100 characters."));
        }

        return new PatientName(firstName, lastName);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FirstName;
        yield return LastName;
    }
}
