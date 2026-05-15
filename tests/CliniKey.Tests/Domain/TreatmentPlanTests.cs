using CliniKey.Domain.Entities;
using CliniKey.Domain.Enums;
using CliniKey.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CliniKey.Tests.Domain;

public class TreatmentPlanTests
{
    [Fact]
    public void Create_WithItems_ComputesTotal()
    {
        // Arrange
        var tooth1 = ToothCode.Create(11).Value;
        var tooth2 = ToothCode.Create(12).Value;
        var cost1 = Money.Create(500m, "EGP").Value;
        var cost2 = Money.Create(700m, "EGP").Value;
        
        var items = new List<(ToothCode, string, Money)>
        {
            (tooth1, "Filling", cost1),
            (tooth2, "Extraction", cost2)
        };

        // Act
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.CalculateTotalEstimatedCost().Value.Amount.Should().Be(1200m);
    }

    [Fact]
    public void Approve_FromProposed_Succeeds()
    {
        // Arrange
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;

        // Act
        var result = plan.Approve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(TreatmentPlanStatus.Approved);
        plan.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TreatmentPlanApprovedEvent");
    }

    [Fact]
    public void Create_EmptyItems_ReturnsFailure()
    {
        // Arrange
        var items = new List<(ToothCode, string, Money)>();

        // Act
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TreatmentPlan.EmptyPlan");
    }

    [Fact]
    public void Approve_FromApproved_ReturnsFailure()
    {
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        plan.Approve();

        var result = plan.Approve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TreatmentPlan.InvalidTransition");
    }

    [Fact]
    public void StartItem_FromProposed_ReturnsFailure()
    {
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        
        var result = plan.StartItem(plan.Items.First().Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TreatmentPlan.InvalidTransition");
    }

    [Fact]
    public void CompleteItem_CompletesAllItems_TransitionsToPlanCompleted()
    {
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        plan.Approve();
        var itemId = plan.Items.First().Id;
        plan.StartItem(itemId);

        var result = plan.CompleteItem(itemId);

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(TreatmentPlanStatus.Completed);
    }

    [Fact]
    public void Cancel_FromInProgress_CancelsNonCompletedItems()
    {
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value),
            (ToothCode.Create(12).Value, "Extraction", Money.Create(700m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        plan.Approve();
        
        var itemsArray = plan.Items.ToArray();
        plan.StartItem(itemsArray[0].Id);
        plan.CompleteItem(itemsArray[0].Id);
        
        plan.StartItem(itemsArray[1].Id);
        
        var result = plan.Cancel();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(TreatmentPlanStatus.Cancelled);
        itemsArray[0].Status.Should().Be(TreatmentItemStatus.Completed);
        itemsArray[1].Status.Should().Be(TreatmentItemStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromCompleted_ReturnsFailure()
    {
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Filling", Money.Create(500m, "EGP").Value)
        };
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items).Value;
        plan.Approve();
        var itemId = plan.Items.First().Id;
        plan.StartItem(itemId);
        plan.CompleteItem(itemId);

        var result = plan.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TreatmentPlan.InvalidTransition");
    }

    [Fact]
    public void InvalidTooth_ReturnsFailure()
    {
        // Act
        var toothResult = ToothCode.Create(99);

        // Assert
        toothResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_MixedCurrencies_ReturnsFailure()
    {
        // Arrange — 5,000 EGP root canal + 200 USD imported crown
        var items = new List<(ToothCode, string, Money)>
        {
            (ToothCode.Create(11).Value, "Root Canal", Money.Create(5000m, "EGP").Value),
            (ToothCode.Create(12).Value, "Imported Crown", Money.Create(200m, "USD").Value)
        };

        // Act
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), items);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TreatmentPlan.MixedCurrencies");
    }
}
