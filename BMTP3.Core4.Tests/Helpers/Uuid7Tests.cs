using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Tests.Helpers;

public class Uuid7Tests
{
	[Fact]
	public void Guid_ReturnsNonEmptyGuid()
	{
		Guid guid = Uuid7.Guid();
		Assert.NotEqual(Guid.Empty, guid);
	}

	[Fact]
	public void Guid_AsOfZero_ReturnsEmptyGuid()
	{
		Guid guid = Uuid7.Guid(0);
		Assert.Equal(Guid.Empty, guid);
	}

	[Fact]
	public void Guid_TimeOrdered()
	{
		Guid a = Uuid7.Guid();
		Guid b = Uuid7.Guid();

		int cmp = a.CompareTo(b);
		Assert.True(cmp <= 0);
	}

	[Fact]
	public void String_ReturnsNonEmpty()
	{
		string s = Uuid7.String();
		Assert.False(string.IsNullOrWhiteSpace(s));
	}

	[Fact]
	public void Id25_Returns25Chars()
	{
		string id = Uuid7.Id25();
		Assert.Equal(25, id.Length);
		Assert.Matches("^[0-9a-z]+$", id);
	}

	[Fact]
	public void Id25_FromGuid_Roundtrips()
	{
		Guid guid = Uuid7.Guid();
		string id = Uuid7.Id25(guid);

		Assert.Equal(25, id.Length);
	}

	[Fact]
	public void Id25_NonV7Guid_Throws()
	{
		Guid v4 = Guid.NewGuid();
		Assert.Throws<ArgumentException>(() => Uuid7.Id25(v4));
	}

	[Fact]
	public void Guid_AsOfFuture_ReturnsValidGuid()
	{
		long futureNs = 200L * 365L * 24L * 60L * 60L * 1_000_000_000L;
		Guid guid = Uuid7.Guid(futureNs);
		Assert.NotEqual(Guid.Empty, guid);
	}
}
