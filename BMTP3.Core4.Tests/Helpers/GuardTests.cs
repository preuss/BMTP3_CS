using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Tests.Helpers;

public class GuardTests
{
	[Fact]
	public void RequireNonNull_WithValue_ReturnsValue()
	{
		object val = new();
		Assert.Same(val, Guard.RequireNonNull(val));
	}

	[Fact]
	public void RequireNonNull_WithNull_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => Guard.RequireNonNull<object>(null!));
	}

	[Fact]
	public void RequireNonNullOrNonWhitespace_WithValidString_ReturnsString()
	{
		string result = Guard.RequireNonNullOrNonWhitespace("hello");
		Assert.Equal("hello", result);
	}

	[Fact]
	public void RequireNonNullOrNonWhitespace_WithNull_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => Guard.RequireNonNullOrNonWhitespace(null!));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void RequireNonNullOrNonWhitespace_WithEmptyOrWhitespace_ThrowsArgumentException(string value)
	{
		Assert.Throws<ArgumentException>(() => Guard.RequireNonNullOrNonWhitespace(value));
	}

	[Fact]
	public void RequireZeroOrGreater_WithZero_ReturnsZero()
	{
		long result = Guard.RequireZeroOrGreater(0);
		Assert.Equal(0, result);
	}

	[Fact]
	public void RequireZeroOrGreater_WithPositive_ReturnsValue()
	{
		long result = Guard.RequireZeroOrGreater(42);
		Assert.Equal(42, result);
	}

	[Fact]
	public void RequireZeroOrGreater_WithNull_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => Guard.RequireZeroOrGreater(null));
	}

	[Fact]
	public void RequireZeroOrGreater_WithNegative_ThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Guard.RequireZeroOrGreater(-1));
	}
}
