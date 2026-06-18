using BMTP3.Core4.Engine.Sidecar.Document;
using BMTP3.Core4.Engine.Sidecar.Writers;
using System.Text;

namespace BMTP3.Core4.Tests.Engine.Sidecar;

public class IniSidecarWriterTests
{
	[Fact]
	public async Task WriteToStreamAsync_WritesIniContent()
	{
		SidecarDocument doc = new();
		doc.WithSection("Source", weight: 10)
			.WithProperty("SourceType", "Drive")
			.WithProperty("SourceFileName", "photo.jpg");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("[Source]", result);
		Assert.Contains("SourceType=Drive", result);
		Assert.Contains("SourceFileName=photo.jpg", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithHeaderComment_WritesHeader()
	{
		SidecarDocument doc = new()
		{
			HeaderComment = ["This is a header", "line two"],
		};
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", "Value");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("# This is a header", result);
		Assert.Contains("# line two", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithSectionComment_WritesComment()
	{
		SidecarDocument doc = new();
		doc.WithSection("Meta", weight: 10, comment: "Section comment here")
			.WithProperty("Key", "Value");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("# Section comment here", result);
		Assert.Contains("[Meta]", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithPropertyComment_WritesComment()
	{
		SidecarDocument doc = new();
		doc.WithSection("Data", weight: 10);
		doc.GetSection("Data")!
			.WithProperty("Key", "Value", comment: "This is a property comment");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("# This is a property comment", result);
		Assert.Contains("Key=Value", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithNullValues_DefaultOptions_WritesEmpty()
	{
		IniSidecarWriter writer = new();
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", (string?)null);

		await using MemoryStream ms = new();
		await writer.WriteToStreamAsync(doc, ms, default);
		ms.Position = 0;

		string result = Encoding.UTF8.GetString(ms.ToArray());

		Assert.Contains("Key=", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithNullValues_SkipOption_SkipsKey()
	{
		var options = new IniSidecarWriterOptions { WriteKeysWithNullValues = false };
		IniSidecarWriter writer = new(options);
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("VisibleKey", "value")
			.WithProperty("HiddenKey", (string?)null);

		string result = await WriteToStreamAsync(doc, writer);

		Assert.Contains("VisibleKey=value", result);
		Assert.DoesNotContain("HiddenKey", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_SectionsSortedByWeight()
	{
		SidecarDocument doc = new();
		doc.WithSection("ZSection", weight: 100)
			.WithProperty("Key", "last");
		doc.WithSection("ASection", weight: 10)
			.WithProperty("Key", "first");

		string result = await WriteToStreamAsync(doc);

		int firstIdx = result.IndexOf("ASection");
		int lastIdx = result.IndexOf("ZSection");
		Assert.True(firstIdx >= 0);
		Assert.True(lastIdx > firstIdx);
	}

	[Fact]
	public async Task WriteToStreamAsync_CancelledToken_Throws()
	{
		IniSidecarWriter writer = new();
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", "Value");

		using CancellationTokenSource cts = new();
		cts.Cancel();

		await using MemoryStream ms = new();
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			writer.WriteToStreamAsync(doc, ms, cts.Token));
	}

	[Fact]
	public async Task WriteToStreamAsync_NullDocument_Throws()
	{
		IniSidecarWriter writer = new();
		await using MemoryStream ms = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			writer.WriteToStreamAsync(null!, ms, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task WriteToStreamAsync_NullStream_Throws()
	{
		IniSidecarWriter writer = new();
		SidecarDocument doc = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			writer.WriteToStreamAsync(doc, null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void WriteToString_ReturnsIniString()
	{
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", "Value");

		IniSidecarWriter writer = new();
		string result = writer.WriteToString(doc);

		Assert.Contains("[Section]", result);
		Assert.Contains("Key=Value", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_EmptyCommentLines_PreservedByDefault()
	{
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10,
			comment: "first\n\nthird")
			.WithProperty("Key", "Value");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("# first", result);
		Assert.Contains("#", result);
		Assert.Contains("# third", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_EmptyCommentLines_SkippedWhenDisabled()
	{
		var options = new IniSidecarWriterOptions { PreserveEmptyCommentLines = false };
		IniSidecarWriter writer = new(options);
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10,
			comment: "first\n\nthird")
			.WithProperty("Key", "Value");

		string result = await WriteToStreamAsync(doc, writer);

		Assert.Contains("# first", result);
		Assert.Contains("# third", result);

		int firstIdx = result.IndexOf("# first");
		int thirdIdx = result.IndexOf("# third");
		int hashOnly = result.IndexOf("#\r\n");
		Assert.True(firstIdx >= 0);
		Assert.True(thirdIdx > firstIdx);
		Assert.Equal(-1, hashOnly);
	}

	[Fact]
	public async Task WriteToStreamAsync_HeaderCommentMultiline_WritesEachLine()
	{
		SidecarDocument doc = new()
		{
			HeaderComment = ["line one\nline two"],
		};
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", "Value");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("# line one", result);
		Assert.Contains("# line two", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_MultipleProperties_AllWritten()
	{
		SidecarDocument doc = new();
		SidecarSection section = doc.WithSection("Section", weight: 10);
		section.WithProperty("A", "1");
		section.WithProperty("B", "2");
		section.WithProperty("C", "3");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("A=1", result);
		Assert.Contains("B=2", result);
		Assert.Contains("C=3", result);
	}

	private static async Task<string> WriteToStreamAsync(SidecarDocument doc, IniSidecarWriter? writer = null)
	{
		writer ??= new IniSidecarWriter();
		await using MemoryStream ms = new();
		await writer.WriteToStreamAsync(doc, ms, TestContext.Current.CancellationToken);
		ms.Position = 0;
		return Encoding.UTF8.GetString(ms.ToArray());
	}
}
