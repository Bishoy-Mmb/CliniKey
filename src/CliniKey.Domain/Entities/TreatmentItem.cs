using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class TreatmentItem : Entity<Guid>
{
    public Guid TreatmentPlanId { get; private set; }
    public ToothCode Tooth { get; private set; }
    public string ProcedureName { get; private set; }
    public Money EstimatedCost { get; private set; }
    public TreatmentItemStatus Status { get; private set; }

    private TreatmentItem()
    {
        ProcedureName = null!;
        EstimatedCost = null!;
        Tooth = null!;
    }

    internal TreatmentItem(
        ToothCode tooth,
        string procedureName,
        Money estimatedCost)
    {
        Id = Guid.NewGuid();
        Tooth = tooth;
        ProcedureName = procedureName;
        EstimatedCost = estimatedCost;
        Status = TreatmentItemStatus.Proposed;
    }

    internal Result Start()
    {
        if (Status != TreatmentItemStatus.Proposed)
            return Result.Failure(Errors.TreatmentPlanErrors.InvalidTransition);

        Status = TreatmentItemStatus.InProgress;
        return Result.Success();
    }

    internal Result Complete()
    {
        if (Status != TreatmentItemStatus.InProgress)
            return Result.Failure(Errors.TreatmentPlanErrors.InvalidTransition);

        Status = TreatmentItemStatus.Completed;
        return Result.Success();
    }

    internal Result Cancel()
    {
        if (Status == TreatmentItemStatus.Completed)
            return Result.Failure(Errors.TreatmentPlanErrors.InvalidTransition);

        Status = TreatmentItemStatus.Cancelled;
        return Result.Success();
    }
}
