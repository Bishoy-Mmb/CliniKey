using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class ToothCodeTests
{
    [Theory]
    [InlineData(11)] // Upper right central incisor
    [InlineData(28)] // Upper left third molar
    [InlineData(34)] // Lower left first premolar
    [InlineData(48)] // Lower right third molar
    public void Create_ValidPermanentFDICode_ReturnsSuccess(int validCode)
    {
        // Act
        var result = ToothCode.Create(validCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validCode);
    }

    [Theory]
    [InlineData(51)] // Upper right deciduous central incisor
    [InlineData(65)] // Upper left deciduous second molar
    [InlineData(73)] // Lower left deciduous canine
    [InlineData(85)] // Lower right deciduous second molar
    public void Create_ValidDeciduousFDICode_ReturnsSuccess(int validCode)
    {
        // Act
        var result = ToothCode.Create(validCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validCode);
    }

    [Theory]
    [InlineData(9)]   // Less than 11
    [InlineData(19)]  // Invalid permanent tooth in quadrant 1
    [InlineData(29)]  // Invalid permanent tooth in quadrant 2
    [InlineData(56)]  // Invalid deciduous tooth in quadrant 5
    [InlineData(91)]  // Invalid quadrant
    [InlineData(99)]  // Random invalid code
    public void Create_InvalidFDICode_ReturnsFailure(int invalidCode)
    {
        // Act
        var result = ToothCode.Create(invalidCode);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("ToothCode.Invalid");
    }
}
