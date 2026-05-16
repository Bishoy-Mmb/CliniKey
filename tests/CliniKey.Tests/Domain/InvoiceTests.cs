using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.Errors;
using CliniKey.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CliniKey.Tests.Domain;

public class InvoiceTests
{
    [Fact]
    public void CreateFromTreatmentPlan_CalculatesVAT()
    {
        // Arrange
        var moneyResult = Money.Create(100m, "EGP");
        var toothCodeResult = ToothCode.Create(11);
        var items = new List<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)> 
        { 
            (toothCodeResult.Value, "Checkup", moneyResult.Value) 
        };
        var planResult = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items);
        var plan = planResult.Value;

        // Act
        var result = Invoice.CreateFromTreatmentPlan(plan);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Lines.Should().HaveCount(1);
        var line = result.Value.Lines.First();
        line.CalculateVatAmount().Value.Amount.Should().Be(14m); // 100 * 0.14
        result.Value.CalculateTotal().Value.Amount.Should().Be(114m);
        result.Value.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void RecordPayment_PartialPayment_StatusPartiallyPaid()
    {
        // Arrange
        var moneyResult = Money.Create(100m, "EGP");
        var toothCodeResult = ToothCode.Create(11);
        var items = new List<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)> 
        { 
            (toothCodeResult.Value, "Checkup", moneyResult.Value) 
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        var invoice = Invoice.CreateFromTreatmentPlan(plan).Value;
        invoice.Issue(); // 114 EGP total

        // Act
        var paymentAmount = Money.Create(50m, "EGP").Value;
        var result = invoice.RecordPayment(paymentAmount, PaymentMethod.Cash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.Payments.Should().HaveCount(1);
    }

    [Fact]
    public void RecordPayment_FullPayment_StatusPaid()
    {
        // Arrange
        var moneyResult = Money.Create(100m, "EGP");
        var toothCodeResult = ToothCode.Create(11);
        var items = new List<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)> 
        { 
            (toothCodeResult.Value, "Checkup", moneyResult.Value) 
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        var invoice = Invoice.CreateFromTreatmentPlan(plan).Value;
        invoice.Issue();

        // Act
        var paymentAmount = Money.Create(114m, "EGP").Value;
        var result = invoice.RecordPayment(paymentAmount, PaymentMethod.Cash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void RecordPayment_Overpayment_ReturnsFailure()
    {
        // Arrange
        var moneyResult = Money.Create(100m, "EGP");
        var toothCodeResult = ToothCode.Create(11);
        var items = new List<(ToothCode Tooth, string ProcedureName, Money EstimatedCost)> 
        { 
            (toothCodeResult.Value, "Checkup", moneyResult.Value) 
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        var invoice = Invoice.CreateFromTreatmentPlan(plan).Value;
        invoice.Issue();

        // Act
        var paymentAmount = Money.Create(150m, "EGP").Value;
        var result = invoice.RecordPayment(paymentAmount, PaymentMethod.Cash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvoiceErrors.Overpayment);
    }
}
