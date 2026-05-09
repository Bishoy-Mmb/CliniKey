using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class PatientNameTests
{
    [Fact]
    public void Create_ValidName_ReturnsSuccess()
    {
        // Act
        var result = PatientName.Create("Ahmed", "Hassan");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Ahmed");
        result.Value.LastName.Should().Be("Hassan");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidFirstName_ReturnsFailure(string? invalidFirstName)
    {
        // Act
        var result = PatientName.Create(invalidFirstName!, "Hassan");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PatientName.InvalidFirstName");
    }

    [Fact]
    public void Create_FirstNameTooLong_ReturnsFailure()
    {
        // Act
        var longName = new string('A', 101);
        var result = PatientName.Create(longName, "Hassan");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PatientName.InvalidFirstName");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidLastName_ReturnsFailure(string? invalidLastName)
    {
        // Act
        var result = PatientName.Create("Ahmed", invalidLastName!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PatientName.InvalidLastName");
    }

    [Fact]
    public void Create_LastNameTooLong_ReturnsFailure()
    {
        // Act
        var longName = new string('A', 101);
        var result = PatientName.Create("Ahmed", longName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("PatientName.InvalidLastName");
    }
}
