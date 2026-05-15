namespace CliniKey.Application.DTOs;

public sealed record TreatmentItemResponse(
    Guid Id,
    string ToothCode,
    string ProcedureName,
    decimal EstimatedCost,
    string Currency,
    string Status);
