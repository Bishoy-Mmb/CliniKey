using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_ValidAmount_ReturnsSuccess()
    {
        // Act
        var result = Money.Create(100.50m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100.50m);
    }

    [Fact]
    public void Create_ZeroAmount_ReturnsSuccess()
    {
        // Act
        var result = Money.Create(0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNoCurrency_DefaultsToEGP()
    {
        // Act
        var result = Money.Create(50m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("EGP");
    }

    [Fact]
    public void Create_NegativeAmount_ReturnsFailure()
    {
        // Act
        var result = Money.Create(-10m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "USD").Value;

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(100m, "EGP").Value;
        var money3 = Money.Create(50m, "USD").Value;

        // Act & Assert
        money1.Should().NotBe(money2);
        money1.Should().NotBe(money3);
        (money1 != money2).Should().BeTrue();
    }
}
