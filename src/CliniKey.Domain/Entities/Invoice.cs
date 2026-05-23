using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.Events;
using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;

namespace CliniKey.Domain.Entities;

public sealed class Invoice : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<InvoiceLine> _lines = [];
    private readonly List<Payment> _payments = [];

    public Guid PatientId { get; private set; }
    public Guid? TreatmentPlanId { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Invoice(TimeProvider clock) : base(clock) { }

    private Invoice() { }

    public static Result<Invoice> CreateFromTreatmentPlan(TreatmentPlan treatmentPlan, TimeProvider clock)
    {
        if (treatmentPlan.Items.Count == 0)
        {
            return Result.Failure<Invoice>(TreatmentPlanErrors.EmptyPlan);
        }

        var invoice = new Invoice(clock)
        {
            Id = Guid.NewGuid(),
            PatientId = treatmentPlan.PatientId,
            TreatmentPlanId = treatmentPlan.Id,
            Status = InvoiceStatus.Draft
        };

        foreach (var item in treatmentPlan.Items)
        {
            invoice._lines.Add(new InvoiceLine(item.ProcedureName, item.EstimatedCost, 0.14m));
        }

        return invoice;
    }

    public Result Issue()
    {
        if (Status != InvoiceStatus.Draft)
        {
            return Result.Failure(InvoiceErrors.InvalidTransition);
        }

        Status = InvoiceStatus.Issued;
        MarkUpdated();

        return Result.Success();
    }

    public Result Void()
    {
        if (Status != InvoiceStatus.Draft && Status != InvoiceStatus.Issued)
        {
            return Result.Failure(InvoiceErrors.CannotVoid);
        }

        Status = InvoiceStatus.Voided;
        MarkUpdated();

        return Result.Success();
    }

    public Result<Money> CalculateSubtotal()
    {
        if (_lines.Count == 0)
        {
            return Money.Zero;
        }

        var total = Money.Create(0m, _lines[0].Amount.Currency).Value;
        foreach (var line in _lines)
        {
            var addResult = total.Add(line.Amount);
            if (addResult.IsFailure) return addResult;
            total = addResult.Value;
        }

        return total;
    }

    public Result<Money> CalculateVatAmount()
    {
        if (_lines.Count == 0)
        {
            return Money.Zero;
        }

        var total = Money.Create(0m, _lines[0].Amount.Currency).Value;
        foreach (var line in _lines)
        {
            var vatAmountResult = line.CalculateVatAmount();
            if (vatAmountResult.IsFailure) return vatAmountResult;

            var addResult = total.Add(vatAmountResult.Value);
            if (addResult.IsFailure) return addResult;
            
            total = addResult.Value;
        }

        return total;
    }

    public Result<Money> CalculateTotal()
    {
        var subtotalResult = CalculateSubtotal();
        if (subtotalResult.IsFailure) return subtotalResult;

        var vatAmountResult = CalculateVatAmount();
        if (vatAmountResult.IsFailure) return vatAmountResult;

        return subtotalResult.Value.Add(vatAmountResult.Value);
    }

    public Result<Money> CalculatePaidAmount()
    {
        if (_payments.Count == 0)
        {
            if (_lines.Count == 0) return Money.Zero;
            return Money.Create(0m, _lines[0].Amount.Currency).Value;
        }

        var total = Money.Create(0m, _payments[0].Amount.Currency).Value;
        foreach (var payment in _payments)
        {
            var addResult = total.Add(payment.Amount);
            if (addResult.IsFailure) return addResult;
            total = addResult.Value;
        }

        return total;
    }

    public Result RecordPayment(Money amount, PaymentMethod method, string? referenceNumber = null)
    {
        if (Status == InvoiceStatus.Draft || Status == InvoiceStatus.Voided)
        {
            return Result.Failure(InvoiceErrors.InvalidTransition);
        }

        if (Status == InvoiceStatus.Paid)
        {
            return Result.Failure(InvoiceErrors.AlreadyPaid);
        }

        var totalResult = CalculateTotal();
        if (totalResult.IsFailure) return totalResult;
        var total = totalResult.Value;

        var paidAmountResult = CalculatePaidAmount();
        if (paidAmountResult.IsFailure) return paidAmountResult;
        var paidAmount = paidAmountResult.Value;

        var remaining = total.Amount - paidAmount.Amount;

        if (amount.Amount > remaining)
        {
            return Result.Failure(InvoiceErrors.Overpayment);
        }

        var now = Clock.GetUtcNow().UtcDateTime;
        var payment = new Payment(amount, method, now, referenceNumber);
        _payments.Add(payment);

        var newPaidAmountResult = CalculatePaidAmount();
        if (newPaidAmountResult.IsFailure) return newPaidAmountResult;
        
        if (newPaidAmountResult.Value.Amount >= total.Amount)
        {
            Status = InvoiceStatus.Paid;
            RaiseDomainEvent(new InvoicePaidEvent(Id, now));
        }
        else
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        MarkUpdated();
        return Result.Success();
    }
}
