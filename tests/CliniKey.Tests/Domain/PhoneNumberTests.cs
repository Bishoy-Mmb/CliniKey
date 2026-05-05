using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("01012345678")]
    [InlineData("01112345678")]
    [InlineData("01212345678")]
    [InlineData("01512345678")]
    public void Create_ValidEgyptianMobile_ReturnsSuccess(string validNumber)
    {
        // Act
        var result = PhoneNumber.Create(validNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validNumber);
    }

    [Theory]
    [InlineData("0101234567")] // 10 digits (too short)
    [InlineData("010123456789")] // 12 digits (too long)
    public void Create_InvalidLength_ReturnsFailure(string invalidNumber)
    {
        // Act
        var result = PhoneNumber.Create(invalidNumber);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PhoneNumber.InvalidFormat");
    }

    [Theory]
    [InlineData("01312345678")] // Invalid prefix 013
    [InlineData("01412345678")] // Invalid prefix 014
    [InlineData("02012345678")] // Starts with 020
    [InlineData("11012345678")] // Doesn't start with 0
    public void Create_InvalidPrefix_ReturnsFailure(string invalidNumber)
    {
        // Act
        var result = PhoneNumber.Create(invalidNumber);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PhoneNumber.InvalidFormat");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyOrNull_ReturnsFailure(string invalidNumber)
    {
        // Act
        var result = PhoneNumber.Create(invalidNumber);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PhoneNumber.Empty");
    }
}
