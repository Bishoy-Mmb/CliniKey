namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Strongly-typed error representation. Replaces raw strings/exceptions
/// with a structured, serializable error model.
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"The {entity} with ID '{id}' was not found.");

    public static Error Validation(string code, string description) =>
        new(code, description);

    public static Error Conflict(string code, string description) =>
        new(code, description);
}
