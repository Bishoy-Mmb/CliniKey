namespace CliniKey.Application.DTOs;

public sealed record TreatmentPlanResponse(
    Guid Id,
    Guid PatientId,
    Guid DentistId,
    string Status,
    decimal TotalEstimatedCost,
    string Currency,
    List<TreatmentItemResponse> Items);
