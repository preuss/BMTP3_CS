using BMTP3.Core4.Engine.Sidecar.Document;
using BMTP3.Core4.Engine.Sidecar.Writers;
using System.Text;

namespace BMTP3.Core4.Tests.Engine.Sidecar;

public class JsonSidecarWriterTests
{
	[Fact]
	public async Task WriteToStreamAsync_WritesJsonContent()
	{
		SidecarDocument doc = new();
		doc.WithSection("Source", weight: 10)
			.WithProperty("SourceType", "Drive")
			.WithProperty("SourceFileName", "photo.jpg");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("\"Source\"", result);
		Assert.Contains("\"SourceType\": \"Drive\"", result);
		Assert.Contains("\"SourceFileName\": \"photo.jpg\"", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithMultipleSections_AllAppear()
	{
		SidecarDocument doc = new();
		doc.WithSection("Source", weight: 10)
			.WithProperty("Name", "test");
		doc.WithSection("Backup", weight: 20)
			.WithProperty("Date", "2026-06-03");
		doc.WithSection("Hashes", weight: 30)
			.WithProperty("SHA2_256", "abc");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("\"Source\"", result);
		Assert.Contains("\"Backup\"", result);
		Assert.Contains("\"Hashes\"", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_WithNullValues_WritesNull()
	{
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("NullKey", (string?)null)
			.WithProperty("StringKey", "value");

		string result = await WriteToStreamAsync(doc);

		Assert.Contains("\"NullKey\": null", result);
		Assert.Contains("\"StringKey\": \"value\"", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_CancelledToken_Throws()
	{
		JsonSidecarWriter writer = new();
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
		JsonSidecarWriter writer = new();
		await using MemoryStream ms = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			writer.WriteToStreamAsync(null!, ms, default));
	}

	[Fact]
	public async Task WriteToStreamAsync_NullStream_Throws()
	{
		JsonSidecarWriter writer = new();
		SidecarDocument doc = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			writer.WriteToStreamAsync(doc, null!, default));
	}

	[Fact]
	public void WriteToString_ReturnsJsonString()
	{
		SidecarDocument doc = new();
		doc.WithSection("Section", weight: 10)
			.WithProperty("Key", "Value");

		string result = JsonSidecarWriter.WriteToString(doc);

		Assert.Contains("\"Section\"", result);
		Assert.Contains("\"Key\": \"Value\"", result);
	}

	[Fact]
	public async Task WriteToStreamAsync_PropertiesSortedByWeight()
	{
		SidecarDocument doc = new();
		SidecarSection section = doc.WithSection("Section", weight: 10);
		section.AddProperty(BMTP3.Core4.Engine.Sidecar.Document.SidecarProperty.From("B", "second", weight: 20));
		section.AddProperty(BMTP3.Core4.Engine.Sidecar.Document.SidecarProperty.From("A", "first", weight: 10));

		string result = await WriteToStreamAsync(doc);

		int firstIdx = result.IndexOf("first");
		int secondIdx = result.IndexOf("second");
		Assert.True(firstIdx >= 0);
		Assert.True(secondIdx > firstIdx);
	}

	[Fact]
	public void WriteToString_NullDocument_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			JsonSidecarWriter.WriteToString(null!));
	}

	private static async Task<string> WriteToStreamAsync(SidecarDocument doc)
	{
		JsonSidecarWriter writer = new();
		await using MemoryStream ms = new();
		await writer.WriteToStreamAsync(doc, ms, default);
		ms.Position = 0;
		return Encoding.UTF8.GetString(ms.ToArray());
	}
}
