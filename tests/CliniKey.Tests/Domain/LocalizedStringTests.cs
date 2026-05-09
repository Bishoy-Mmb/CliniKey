using CliniKey.Domain.ValueObjects;
using CliniKey.SharedKernel.Primitives;
using FluentAssertions;

namespace CliniKey.Tests.Domain;

public class LocalizedStringTests
{
    [Fact]
    public void Create_ValidEnglishOnly_ReturnsSuccess()
    {
        // Act
        var result = LocalizedString.Create("Tooth");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.En.Should().Be("Tooth");
        result.Value.Ar.Should().BeNull();
    }

    [Fact]
    public void Create_ValidEnglishAndArabic_ReturnsSuccess()
    {
        // Act
        var result = LocalizedString.Create("Tooth", "سنة");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.En.Should().Be("Tooth");
        result.Value.Ar.Should().Be("سنة");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidEnglish_ReturnsFailure(string? invalidEnglish)
    {
        // Act
        var result = LocalizedString.Create(invalidEnglish!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("LocalizedString.EmptyEnglish");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyOrWhitespaceArabic_ConvertsToNull(string invalidArabic)
    {
        // Act
        var result = LocalizedString.Create("Tooth", invalidArabic);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ar.Should().BeNull();
    }

    [Fact]
    public void Equality_DifferentArabicValues_ReturnsFalse()
    {
        // Arrange
        var localized1 = LocalizedString.Create("Hello", "مرحبا").Value;
        var localized2 = LocalizedString.Create("Hello", null).Value;

        // Act & Assert
        localized1.Should().NotBe(localized2);
        (localized1 != localized2).Should().BeTrue();
    }
}
