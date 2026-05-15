using System.Data;
using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Messaging;
using CliniKey.Application.DTOs;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Dapper;

namespace CliniKey.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

internal sealed class GetTreatmentPlanByIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTreatmentPlanByIdQuery, TreatmentPlanResponse>
{
    private sealed record TreatmentPlanRow(
        Guid Id, Guid PatientId, Guid DentistId, int Status);

    private sealed record TreatmentItemRow(
        Guid? ItemId, int? ToothCode, string? ProcedureName,
        decimal? EstimatedCost, string? Currency, int? ItemStatus);

    public async Task<Result<TreatmentPlanResponse>> Handle(GetTreatmentPlanByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                tp.id AS Id, 
                tp.patient_id AS PatientId, 
                tp.dentist_id AS DentistId, 
                tp.status AS Status,
                i.id AS ItemId,
                i.tooth_code AS ToothCode,
                i.procedure_name AS ProcedureName,
                i.estimated_cost_amount AS EstimatedCost,
                i.estimated_cost_currency AS Currency,
                i.status AS ItemStatus
            FROM treatment_plans tp
            LEFT JOIN treatment_items i ON tp.id = i.treatment_plan_id
            WHERE tp.id = @TreatmentPlanId
            """;

        var lookup = new Dictionary<Guid, TreatmentPlanResponse>();

        await connection.QueryAsync<TreatmentPlanRow, TreatmentItemRow, TreatmentPlanResponse>(
            sql,
            (plan, item) =>
            {
                Guid planId = plan.Id;
                if (!lookup.TryGetValue(planId, out TreatmentPlanResponse? planResponse))
                {
                    planResponse = new TreatmentPlanResponse(
                        planId,
                        plan.PatientId,
                        plan.DentistId,
                        ((TreatmentPlanStatus)plan.Status).ToString(),
                        0m, // Computed after processing items
                        "EGP",
                        new List<TreatmentItemResponse>()
                    );
                    lookup.Add(plan.Id, planResponse);
                }

                if (item is not null && item.ItemId.HasValue)
                {
                    planResponse.Items.Add(new TreatmentItemResponse(
                        item.ItemId.Value,
                        item.ToothCode!.Value.ToString(),
                        item.ProcedureName!,
                        item.EstimatedCost!.Value,
                        item.Currency!,
                        ((TreatmentItemStatus)item.ItemStatus!.Value).ToString()
                    ));
                }

                return planResponse;
            },
            new { request.TreatmentPlanId },
            splitOn: "ItemId"
        );

        if (!lookup.TryGetValue(request.TreatmentPlanId, out TreatmentPlanResponse? result))
        {
            return Result.Failure<TreatmentPlanResponse>(TreatmentPlanErrors.NotFound(request.TreatmentPlanId));
        }

        var totalCost = result.Items.Sum(i => i.EstimatedCost);
        var currency = result.Items.FirstOrDefault()?.Currency ?? "EGP";

        var finalResponse = result with { TotalEstimatedCost = totalCost, Currency = currency };

        return finalResponse;
    }
}
