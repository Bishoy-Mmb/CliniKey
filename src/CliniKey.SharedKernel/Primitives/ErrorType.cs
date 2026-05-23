namespace CliniKey.SharedKernel.Primitives;

/// <summary>
/// Classifies domain errors so the API layer can map them
/// to the correct HTTP status code without string matching.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
