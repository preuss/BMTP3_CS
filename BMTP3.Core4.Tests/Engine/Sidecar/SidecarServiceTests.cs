using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Hashing;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine.Sidecar;

public class SidecarServiceTests
{
	private static readonly SidecarService Service = new(NullLogger<SidecarService>.Instance);

	[Fact]
	public async Task WriteAsync_IniFormat_CreatesSidecarFile()
	{
		using TempDirectory temp = new();
		string targetPath = Path.Combine(temp.Path, "photo.jpg");

		await Service.WriteAsync(targetPath, SampleRequest(SidecarFormat.Ini), default);

		string sidecarPath = targetPath + ".sidecar.ini";
		Assert.True(File.Exists(sidecarPath));

		string content = await File.ReadAllTextAsync(sidecarPath);
		Assert.Contains("[Source]", content);
		Assert.Contains("[Backup]", content);
		Assert.Contains("[Path]", content);
		Assert.Contains("[Hashes]", content);
		Assert.Contains("SourceType=Drive", content);
		Assert.Contains("SourceFileName=photo.jpg", content);
		Assert.Contains("SHA2_256=abc123", content);
		Assert.Contains("MD5=def456", content);
	}

	[Fact]
	public async Task WriteAsync_JsonFormat_CreatesSidecarFile()
	{
		using TempDirectory temp = new();
		string targetPath = Path.Combine(temp.Path, "photo.jpg");

		await Service.WriteAsync(targetPath, SampleRequest(SidecarFormat.Json), default);

		string sidecarPath = targetPath + ".sidecar.json";
		Assert.True(File.Exists(sidecarPath));

		string content = await File.ReadAllTextAsync(sidecarPath);
		Assert.Contains("\"Source\"", content);
		Assert.Contains("\"Backup\"", content);
		Assert.Contains("\"Path\"", content);
		Assert.Contains("\"Hashes\"", content);
		Assert.Contains("\"SourceType\": \"Drive\"", content);
		Assert.Contains("\"SourceFileName\": \"photo.jpg\"", content);
	}

	[Fact]
	public async Task WriteAsync_CancelledToken_ThrowsOperationCanceled()
	{
		using TempDirectory temp = new();
		string targetPath = Path.Combine(temp.Path, "photo.jpg");
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			Service.WriteAsync(targetPath, SampleRequest(SidecarFormat.Ini), cts.Token));
	}

	[Fact]
	public async Task WriteAsync_NullTargetFilePath_Throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			Service.WriteAsync(null!, SampleRequest(SidecarFormat.Ini), default));
	}

	[Fact]
	public async Task WriteAsync_NullRequest_Throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			Service.WriteAsync("somepath", null!, default));
	}

	[Fact]
	public async Task WriteAsync_WithAllOptionalFields_WritesCompleteContent()
	{
		using TempDirectory temp = new();
		string targetPath = Path.Combine(temp.Path, "vacation.mp4");

		var request = new SidecarRequest
		{
			Format = SidecarFormat.Ini,
			SourceType = "MtpDevice",
			SourceFileName = "vacation.mp4",
			SourcePersistentUniqueId = "MTP:12345",
			SourceFullPath = @"Computer\Phone\DCIM\vacation.mp4",
			MediaTakenDateTime = new DateTimeOffset(2026, 6, 1, 14, 30, 0, TimeSpan.Zero),
			AuthoredDateTime = new DateTimeOffset(2026, 6, 1, 14, 28, 0, TimeSpan.Zero),
			CreateDateTime = new DateTimeOffset(2026, 6, 1, 14, 30, 0, TimeSpan.Zero),
			LastWriteDateTime = new DateTimeOffset(2026, 6, 1, 14, 32, 0, TimeSpan.Zero),
			LastAccessDateTime = new DateTimeOffset(2026, 6, 1, 14, 32, 0, TimeSpan.Zero),
			BackupStartDateTime = new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero),
			SourceRelativePath = @"Phone\DCIM\vacation.mp4",
			SanitizedSourceRelativePath = @"Phone\DCIM\vacation.mp4",
			TargetRelativePath = @"2026\06\vacation.mp4",
			Hashes = new Dictionary<HashType, string>
			{
				[HashType.SHA3_512_FIPS202] = "fips202hash",
				[HashType.BLAKE3_512] = "blake3hash",
			},
			SourceDetailsSectionName = "SourceDevice",
			SourceDetails = new Dictionary<string, string>
			{
				["DeviceName"] = "MyPhone",
				["DeviceSerial"] = "ABC123",
			},
		};

		await Service.WriteAsync(targetPath, request, default);

		string sidecarPath = targetPath + ".sidecar.ini";
		Assert.True(File.Exists(sidecarPath));

		string content = await File.ReadAllTextAsync(sidecarPath);
		Assert.Contains("[SourceDevice]", content);
		Assert.Contains("DeviceName=MyPhone", content);
		Assert.Contains("DeviceSerial=ABC123", content);
		Assert.Contains("SHA3_512=fips202hash", content);
		Assert.Contains("BLAKE3_512=blake3hash", content);
	}

	private static SidecarRequest SampleRequest(SidecarFormat format) => new()
	{
		Format = format,
		SourceType = "Drive",
		SourceFileName = "photo.jpg",
		BackupStartDateTime = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero),
		SourceRelativePath = @"photos\photo.jpg",
		TargetRelativePath = @"photos\photo.jpg",
		Hashes = new Dictionary<HashType, string>
		{
			[HashType.SHA2_256] = "abc123",
			[HashType.MD5_128] = "def456",
		},
	};

	private sealed class TempDirectory : IDisposable
	{
		public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BMTP3_Test_" + Guid.NewGuid().ToString("N"));

		public TempDirectory()
		{
			Directory.CreateDirectory(Path);
		}

		public void Dispose()
		{
			try { Directory.Delete(Path, recursive: true); }
			catch { /* ignore cleanup failures */ }
		}
	}
}
