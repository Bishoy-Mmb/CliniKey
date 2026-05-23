using System.Collections.Generic;
using System.Linq;

namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Strongly-typed error representation. Replaces raw strings/exceptions
/// with a structured, serializable error model.
/// The <see cref="Type"/> discriminator enables the API layer to map
/// errors to HTTP status codes without string matching.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.", ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Validation(IEnumerable<(string property, string message)> errors) =>
        new("Validation.Error",
            string.Join(", ", errors.Select(e => $"{e.property}: {e.message}")),
            ErrorType.Validation);

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"The {entity} with ID '{id}' was not found.", ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);
}
