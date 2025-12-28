using FluentAssertions;
using Moxd.Guards;

namespace Moxd.Maui.Core.Tests;

public class GuardTests
{
    [Fact]
    public void IsNotNull_WithNonNullValue_ReturnsValue()
    {
        // Arrange
        string value = "test";

        // Act
        string result = Guard.IsNotNull(value);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void IsNotNull_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Func<string> act = () => Guard.IsNotNull(value);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void IsNotNull_WithNullObject_ThrowsWithCorrectParameterName()
    {
        // Arrange
        object? myParameter = null;

        // Act
        Func<object> act = () => Guard.IsNotNull(myParameter);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("myParameter");
    }

    [Fact]
    public void IsNotNullOrEmpty_WithValidString_ReturnsString()
    {
        // Arrange
        string value = "hello";

        // Act
        string result = Guard.IsNotNullOrEmpty(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsNotNullOrEmpty_WithNullOrEmptyString_ThrowsArgumentException(string? value)
    {
        // Act
        Func<string> act = () => Guard.IsNotNullOrEmpty(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_WithValidString_ReturnsString()
    {
        // Arrange
        string value = "hello";

        // Act
        string result = Guard.IsNotNullOrWhiteSpace(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void IsNotNullOrWhiteSpace_WithInvalidString_ThrowsArgumentException(string? value)
    {
        // Act
        Func<string> act = () => Guard.IsNotNullOrWhiteSpace(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*cannot be null, empty, or whitespace*");
    }

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void IsInRange_WithValueInRange_ReturnsValue(int value, int min, int max)
    {
        // Act
        int result = Guard.IsInRange(value, min, max);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    [InlineData(-5, 0, 100)]
    public void IsInRange_WithValueOutOfRange_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        // Act
        Func<int> act = () => Guard.IsInRange(value, min, max);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*must be between {min} and {max}*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void IsPositive_WithPositiveValue_ReturnsValue(int value)
    {
        // Act
        int result = Guard.IsPositive(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void IsPositive_WithNonPositiveValue_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        Func<int> act = () => Guard.IsPositive(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*must be positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void IsNotNegative_WithNonNegativeValue_ReturnsValue(int value)
    {
        // Act
        int result = Guard.IsNotNegative(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void IsNotNegative_WithNegativeValue_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        Func<int> act = () => Guard.IsNotNegative(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void IsNotNullOrEmpty_Collection_WithItems_ReturnsCollection()
    {
        // Arrange
        List<int> collection = [1, 2, 3];

        // Act
        IEnumerable<int> result = Guard.IsNotNullOrEmpty(collection);

        // Assert
        result.Should().BeEquivalentTo(collection);
    }

    [Fact]
    public void IsNotNullOrEmpty_Collection_WithNull_ThrowsArgumentException()
    {
        // Arrange
        List<int>? collection = null;

        // Act
        Func<IEnumerable<int>> act = () => Guard.IsNotNullOrEmpty(collection);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void IsNotNullOrEmpty_Collection_WithEmpty_ThrowsArgumentException()
    {
        // Arrange
        List<int> collection = [];

        // Act
        Func<IEnumerable<int>> act = () => Guard.IsNotNullOrEmpty(collection);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void IsTrue_WithTrueCondition_DoesNotThrow()
    {
        // Act
        Action act = () => Guard.IsTrue(true, "Condition should be true");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsTrue_WithFalseCondition_ThrowsArgumentException()
    {
        // Act
        Action act = () => Guard.IsTrue(false, "Condition must be true");

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Condition must be true*");
    }

    [Fact]
    public void IsFalse_WithFalseCondition_DoesNotThrow()
    {
        // Act
        Action act = () => Guard.IsFalse(false, "Condition should be false");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsFalse_WithTrueCondition_ThrowsArgumentException()
    {
        // Act
        Action act = () => Guard.IsFalse(true, "Condition must be false");

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Condition must be false*");
    }
}