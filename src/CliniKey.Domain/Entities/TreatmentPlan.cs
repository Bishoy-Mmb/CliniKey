using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class TreatmentPlan : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<TreatmentItem> _items = [];

    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public TreatmentPlanStatus Status { get; private set; }
    
    public IReadOnlyCollection<TreatmentItem> Items => _items.AsReadOnly();

    public Result<Money> CalculateTotalEstimatedCost()
    {
        if (_items.Count == 0)
        {
            return Money.Zero;
        }

        var total = Money.Create(0m, _items[0].EstimatedCost.Currency);
        if (total.IsFailure)
        {
            return total;
        }

        foreach (var item in _items)
        {
            var addResult = total.Value.Add(item.EstimatedCost);
            if (addResult.IsFailure)
            {
                return addResult;
            }

            total = addResult;
        }

        return total;
    }

    private TreatmentPlan()
    {
    }

    public static Result<TreatmentPlan> Create(Guid patientId, Guid dentistId, IEnumerable<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return Result.Failure<TreatmentPlan>(TreatmentPlanErrors.EmptyPlan);
        }

        var firstCurrency = itemList[0].EstimatedCost.Currency;
        if (itemList.Any(i => !string.Equals(i.EstimatedCost.Currency, firstCurrency, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<TreatmentPlan>(TreatmentPlanErrors.MixedCurrencies);
        }

        var plan = new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            Status = TreatmentPlanStatus.Proposed
        };

        foreach (var item in itemList)
        {
            plan._items.Add(new TreatmentItem(item.Tooth, item.ProcedureName, item.EstimatedCost));
        }

        plan.RaiseDomainEvent(new TreatmentPlanCreatedEvent(plan.Id, DateTime.UtcNow));

        return plan;
    }

    public Result Approve()
    {
        if (Status != TreatmentPlanStatus.Proposed)
        {
            return Result.Failure(TreatmentPlanErrors.InvalidTransition);
        }

        Status = TreatmentPlanStatus.Approved;
        RaiseDomainEvent(new TreatmentPlanApprovedEvent(Id, DateTime.UtcNow));
        MarkUpdated();

        return Result.Success();
    }

    public Result StartItem(Guid itemId)
    {
        if (Status != TreatmentPlanStatus.Approved && Status != TreatmentPlanStatus.InProgress)
        {
            return Result.Failure(TreatmentPlanErrors.InvalidTransition);
        }

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return Result.Failure(TreatmentPlanErrors.NotFound(itemId));
        }

        var result = item.Start();
        if (result.IsFailure)
        {
            return result;
        }

        if (Status == TreatmentPlanStatus.Approved)
        {
            Status = TreatmentPlanStatus.InProgress;
        }

        MarkUpdated();
        return Result.Success();
    }

    public Result CompleteItem(Guid itemId)
    {
        if (Status != TreatmentPlanStatus.InProgress)
        {
            return Result.Failure(TreatmentPlanErrors.InvalidTransition);
        }

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return Result.Failure(TreatmentPlanErrors.NotFound(itemId));
        }

        var result = item.Complete();
        if (result.IsFailure)
        {
            return result;
        }

        if (_items.All(i => i.Status == TreatmentItemStatus.Completed || i.Status == TreatmentItemStatus.Cancelled))
        {
            Status = TreatmentPlanStatus.Completed;
        }

        MarkUpdated();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == TreatmentPlanStatus.Completed || Status == TreatmentPlanStatus.Cancelled)
        {
            return Result.Failure(TreatmentPlanErrors.InvalidTransition);
        }

        Status = TreatmentPlanStatus.Cancelled;
        foreach (var item in _items.Where(i => i.Status != TreatmentItemStatus.Completed && i.Status != TreatmentItemStatus.Cancelled))
        {
            item.Cancel();
        }

        MarkUpdated();
        return Result.Success();
    }
}
