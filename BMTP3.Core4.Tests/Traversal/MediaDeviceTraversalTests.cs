using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Devices;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Tests.Fakes;
using BMTP3.Core4.Traversal;
using System.Reflection;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceTraversalTests
{
	// ==============================================
	// Constructor
	// ==============================================

	[Fact]
	public void Constructor_NullDevice_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceTraversal(null!, new FakeGatekeeper()));
	}

	// ==============================================
	// GenerateDeviceUniqueId
	// ==============================================

	[Fact]
	public void GenerateDeviceUniqueId_NormalCase()
	{
		string id = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(
			"/DCIM/IMG_001.jpg", 12345,
			new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 1, 15, 14, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2024, 1, 14, 8, 0, 0, TimeSpan.Zero));

		Assert.StartsWith("mtp-stable-v1_", id);
		Assert.Contains("/DCIM/IMG_001.jpg", id);
		Assert.Contains("12345", id);
	}

	[Fact]
	public void GenerateDeviceUniqueId_AllTimestampsNull()
	{
		string id = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(
			"/DCIM/IMG_001.jpg", 12345, null, null, null);

		Assert.StartsWith("mtp-stable-v1_", id);
		// All null timestamps produce empty segments → double underscore separators
		Assert.EndsWith("___", id);
	}

	[Fact]
	public void GenerateDeviceUniqueId_SomeTimestampsNull()
	{
		string id = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(
			"/DCIM/IMG_001.jpg", 12345,
			new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
			null,
			new DateTimeOffset(2024, 1, 14, 8, 0, 0, TimeSpan.Zero));

		Assert.StartsWith("mtp-stable-v1_", id);
	}

	[Fact]
	public void GenerateDeviceUniqueId_NullPath_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(null!, 0, null, null, null));
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("\t")]
	public void GenerateDeviceUniqueId_EmptyOrWhitespacePath_Throws(string path)
	{
		Assert.Throws<ArgumentException>(() =>
			MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(path, 0, null, null, null));
	}

	[Fact]
	public void GenerateDeviceUniqueId_ProducesDeterministicOutput()
	{
		DateTimeOffset ts = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

		string id1 = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId("/A.jpg", 100, ts, ts, ts);
		string id2 = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId("/A.jpg", 100, ts, ts, ts);

		Assert.Equal(id1, id2);
	}

	[Fact]
	public void GenerateDeviceUniqueId_DifferentSize_ProducesDifferentId()
	{
		DateTimeOffset ts = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
		string id1 = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId("/A.jpg", 100, ts, ts, ts);
		string id2 = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId("/A.jpg", 200, ts, ts, ts);
		Assert.NotEqual(id1, id2);
	}

	[Fact]
	public void GenerateDeviceUniqueId_ZeroSize()
	{
		string id = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId("/empty.txt", 0, null, null, null);
		Assert.StartsWith("mtp-stable-v1_", id);
	}

	[Fact]
	public void GenerateDeviceUniqueId_VeryLongPath()
	{
		string longPath = "/" + new string('A', 500) + ".txt";
		string id = MediaDeviceTraversal_Accessor.GenerateDeviceUniqueId(longPath, 1, null, null, null);
		Assert.StartsWith("mtp-stable-v1_", id);
		Assert.Contains(longPath, id);
	}

	// ==============================================
	// ToUtcOffsetOrNull
	// ==============================================

	[Fact]
	public void ToUtcOffsetOrNull_NullInput_ReturnsNull()
	{
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(null);
		Assert.Null(result);
	}

	[Fact]
	public void ToUtcOffsetOrNull_UtcKind()
	{
		DateTime dt = new(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(dt);
		Assert.NotNull(result);
		Assert.Equal(new DateTimeOffset(dt, TimeSpan.Zero), result);
	}

	[Fact]
	public void ToUtcOffsetOrNull_LocalKind()
	{
		DateTime dt = new(2024, 6, 15, 10, 0, 0, DateTimeKind.Local);
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(dt);
		Assert.NotNull(result);
		Assert.Equal(dt.ToUniversalTime(), result.Value.DateTime);
		Assert.Equal(TimeSpan.Zero, result.Value.Offset);
	}

	[Fact]
	public void ToUtcOffsetOrNull_UnspecifiedKind_TreatedAsLocal()
	{
		DateTime dt = new(2024, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(dt);
		Assert.NotNull(result);
		DateTime localInterpreted = DateTime.SpecifyKind(dt, DateTimeKind.Local);
		Assert.Equal(localInterpreted.ToUniversalTime(), result.Value.DateTime);
	}

	[Fact]
	public void ToUtcOffsetOrNull_MinValue()
	{
		DateTime dt = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(dt);
		Assert.NotNull(result);
		Assert.Equal(DateTimeOffset.MinValue, result);
	}

	[Fact]
	public void ToUtcOffsetOrNull_MaxValue()
	{
		DateTime dt = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
		DateTimeOffset? result = MediaDeviceTraversal_Accessor.ToUtcOffsetOrNull(dt);
		Assert.NotNull(result);
		Assert.Equal(DateTimeOffset.MaxValue, result);
	}

	// ==============================================
	// PathHelper.NormalizePath
	// ==============================================

	[Fact]
	public void NormalizePath_ForwardSlash()
	{
		string result = PathHelper.NormalizePath("DCIM/Camera");
		Assert.Equal(@"DCIM\Camera", result);
	}

	[Fact]
	public void NormalizePath_BackslashStays()
	{
		string result = PathHelper.NormalizePath(@"DCIM\Camera");
		Assert.Equal(@"DCIM\Camera", result);
	}

	[Fact]
	public void NormalizePath_MixedSeparators()
	{
		string result = PathHelper.NormalizePath("DCIM/Camera\\2024");
		Assert.Equal(@"DCIM\Camera\2024", result);
	}

	[Fact]
	public void NormalizePath_LeadingBackslash()
	{
		string result = PathHelper.NormalizePath(@"\DCIM\Camera");
		Assert.Equal(@"DCIM\Camera", result);
	}

	[Fact]
	public void NormalizePath_TrailingBackslash()
	{
		string result = PathHelper.NormalizePath(@"DCIM\Camera\");
		Assert.Equal(@"DCIM\Camera", result);
	}

	[Fact]
	public void NormalizePath_LeadingAndTrailingBackslash()
	{
		string result = PathHelper.NormalizePath(@"\DCIM\Camera\");
		Assert.Equal(@"DCIM\Camera", result);
	}

	[Fact]
	public void NormalizePath_Empty_ReturnsEmpty()
	{
		string result = PathHelper.NormalizePath("");
		Assert.Equal("", result);
	}

	[Fact]
	public void NormalizePath_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			PathHelper.NormalizePath(null!));
	}

	[Fact]
	public void NormalizePath_OnlyBackslashes()
	{
		string result = PathHelper.NormalizePath(@"\\\");
		Assert.Equal("", result);
	}

	[Fact]
	public void NormalizePath_Unicode()
	{
		string result = PathHelper.NormalizePath("fotos/øebleskiver");
		Assert.Equal(@"fotos\øebleskiver", result);
	}

	// ==============================================
	// PathHelper.NormalizePatterns
	// ==============================================

	[Fact]
	public void NormalizePatterns_Null_ReturnsNull()
	{
		Assert.Null(PathHelper.NormalizePatterns(null));
	}

	[Fact]
	public void NormalizePatterns_Empty_ReturnsSame()
	{
		string[] empty = Array.Empty<string>();
		IReadOnlyList<string>? result = PathHelper.NormalizePatterns(empty);
		Assert.Same(empty, result);
	}

	[Fact]
	public void NormalizePatterns_NormalizesSlashes()
	{
		string[] patterns = new[] { "DCIM/Camera/*.jpg", "*.txt" };
		IReadOnlyList<string>? result = PathHelper.NormalizePatterns(patterns);
		Assert.Equal("DCIM/Camera/*.jpg", result![0]);
		Assert.Equal("*.txt", result[1]);
	}

	[Fact]
	public void NormalizePatterns_SinglePattern()
	{
		string[] patterns = new[] { "*.jpg" };
		IReadOnlyList<string>? result = PathHelper.NormalizePatterns(patterns);
		Assert.Equal(new[] { "*.jpg" }, result);
	}

	// ==============================================
	// TraverseAsync — ItemIdScope
	// ==============================================

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Session_UsesFileId()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		FakeMediaFile file = new(
			id: "session-obj-123",
			persistentUniqueId: "puid-456",
			name: "test.jpg",
			fullName: "/test.jpg",
			length: 100);
		dir.AddFile(file);

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = ItemIdScope.Session,
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("session-obj-123", items[0].Id);
	}

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Connection_UsesPuid()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		FakeMediaFile file = new(
			id: "session-obj-123",
			persistentUniqueId: "puid-456",
			name: "test.jpg",
			fullName: "/test.jpg",
			length: 100);
		dir.AddFile(file);

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = ItemIdScope.Connection,
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("puid-456", items[0].Id);
	}

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Persistent_UsesGeneratedId()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		FakeMediaFile file = new(
			id: "obj-1", persistentUniqueId: "puid-1",
			name: "test.jpg", fullName: "/test.jpg",
			length: 100,
			created: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			modified: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
			authored: new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
		dir.AddFile(file);

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = ItemIdScope.Persistent,
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.StartsWith("mtp-stable-v1_", items[0].Id);
		Assert.Contains("/test.jpg", items[0].Id);
	}

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Default_FallbackToPuid()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		dir.AddFile(new FakeMediaFile(
			id: "obj-1", persistentUniqueId: "puid-default",
			name: "f.jpg", fullName: "/f.jpg", length: 1));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = (ItemIdScope)999, // unknown value → default branch
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("puid-default", items[0].Id);
	}

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Session_NullId_Throws()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		dir.AddFile(new FakeMediaFile(
			id: null!, persistentUniqueId: "puid",
			name: "f.jpg", fullName: "/f.jpg", length: 1));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = ItemIdScope.Session,
			Recursive = false,
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task TraverseAsync_ItemIdScope_Connection_NullPuid_Throws()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		dir.AddFile(new FakeMediaFile(
			id: "obj-1", persistentUniqueId: null!,
			name: "f.jpg", fullName: "/f.jpg", length: 1));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			ItemIdScope = ItemIdScope.Connection,
			Recursive = false,
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken));
	}

	// ==============================================
	// TraverseAsync — Recursive
	// ==============================================

	[Fact]
	public async Task TraverseAsync_NonRecursive_OnlyRootFiles()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(2, items.Count);
		Assert.Contains(items, i => i.RelativeFilePath == @"root.jpg");
		Assert.Contains(items, i => i.RelativeFilePath == @"notes.txt");
	}

	[Fact]
	public async Task TraverseAsync_Recursive_FindsAllFiles()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = true,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		// root.jpg, notes.txt, sub1.jpg, readme.txt, nested.txt = 5 files
		Assert.Equal(5, items.Count);
	}

	// ==============================================
	// TraverseAsync — SubPath
	// ==============================================

	[Fact]
	public async Task TraverseAsync_SubPath_NavigatesToSubDirectory()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive/SubDir1",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(2, items.Count);
	}

	[Fact]
	public async Task TraverseAsync_SubPath_Nested()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive/SubDir1/NestedDir",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(1, items.Count);
		Assert.Equal("nested.txt", items[0].FileName);
	}

	[Fact]
	public async Task TraverseAsync_SubPath_NotFound_Throws()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive/NonExistent",
			Recursive = false,
		};

		await Assert.ThrowsAsync<DirectoryNotFoundException>(() => CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken));
	}

	// ==============================================
	// TraverseAsync — Glob filtering
	// ==============================================

	[Fact]
	public async Task TraverseAsync_IncludePattern_Filters()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
			IncludePatterns = new[] { "*.jpg" },
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("root.jpg", items[0].FileName);
	}

	[Fact]
	public async Task TraverseAsync_ExcludePattern_Filters()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
			ExcludePatterns = new[] { "*.jpg" },
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("notes.txt", items[0].FileName);
	}

	[Fact]
	public async Task TraverseAsync_IncludePatternWithForwardSlash()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = true,
			IncludePatterns = new[] { "SubDir1/*" },
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(2, items.Count);
		Assert.All(items, i => Assert.StartsWith("SubDir1/", i.RelativeFilePath));
	}

	// ==============================================
	// TraverseAsync — Progress reporting
	// ==============================================

	[Fact]
	public async Task TraverseAsync_ReportsProgress()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();

		List<SourceTraversalProgress> reported = new();
		IProgress<SourceTraversalProgress> progress = new TestProgress<SourceTraversalProgress>(reported);

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = true,
		};

		await CollectItems(traversal, request, progress, cancellationToken: TestContext.Current.CancellationToken);
		Assert.NotEmpty(reported);
		Assert.True(reported.Last().FilesDiscovered >= 4);
	}

	// ==============================================
	// TraverseAsync — Cancellation
	// ==============================================

	[Fact]
	public async Task TraverseAsync_Cancellation_StopsEnumeration()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory root) = CreateTraversalWithSubDirs();
		using CancellationTokenSource cts = new();
		cts.Cancel(); // pre-cancelled

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = true,
		};

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			CollectItems(traversal, request, cancellationToken: cts.Token));
	}

	// ==============================================
	// TraverseAsync — Timestamps populated
	// ==============================================

	[Fact]
	public async Task TraverseAsync_PopulatesTimestamps()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		DateTime created = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		DateTime modified = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
		DateTime authored = new(2024, 3, 20, 8, 0, 0, DateTimeKind.Utc);

		dir.AddFile(new FakeMediaFile(
			id: "o1", persistentUniqueId: "p1",
			name: "f.jpg", fullName: "/f.jpg", length: 1,
			created: created, modified: modified, authored: authored));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.NotNull(items[0].DateCreated);
		Assert.NotNull(items[0].DateModified);
		Assert.NotNull(items[0].DateAuthored);
		Assert.Null(items[0].DateAccessed);
	}

	// ==============================================
	// TraverseAsync — FileName preserved (no normalization)
	// ==============================================

	[Fact]
	public async Task TraverseAsync_FileName_PreservesOriginalName()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal();
		dir.AddFile(new FakeMediaFile(
			id: "o1", persistentUniqueId: "p1",
			name: "IMAG0034.jpg", fullName: "/DCIM/IMAG0034.jpg", length: 99));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.Equal("IMAG0034.jpg", items[0].FileName);
	}

	// ==============================================
	// TraverseAsync — SourcePath format
	// ==============================================

	[Fact]
	public async Task TraverseAsync_SourcePath_UsesMtpScheme()
	{
		(MediaDeviceTraversal traversal, FakeMediaDirectory dir) = CreateSimpleTraversal("MyPhone", "SD Card");
		dir.AddFile(new FakeMediaFile(
			id: "o1", persistentUniqueId: "p1",
			name: "doc.pdf", fullName: "/DCIM/doc.pdf", length: 50));

		SourceTraversalRequest request = new()
		{
			SourcePath = "mtp://Device/Drive",
			Recursive = false,
		};

		List<SourceTraversalItem> items = await CollectItems(traversal, request, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(items);
		Assert.StartsWith("mtp://Device/Drive/", items[0].SourcePath);
	}

	// ==============================================
	// Helpers
	// ==============================================

	private static async Task<List<SourceTraversalItem>> CollectItems(
		MediaDeviceTraversal traversal,
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		List<SourceTraversalItem> items = new();
		await foreach(SourceTraversalItem item in traversal.TraverseAsync(request, progress, cancellationToken))
			items.Add(item);
		return items;
	}

	private static (MediaDeviceTraversal, FakeMediaDirectory) CreateSimpleTraversal(
		string deviceName = "Device", string driveName = "Drive")
	{
		FakeMediaDirectory rootDir = new("");
		FakeMediaDrive drive = new(driveName, rootDir);
		FakeMediaDevice device = new(deviceName);
		FakeConnectedMediaDriveSource source = new(device, drive);
		return (new MediaDeviceTraversal(source, new FakeGatekeeper()), rootDir);
	}

	private static (MediaDeviceTraversal, FakeMediaDirectory) CreateTraversalWithSubDirs()
	{
		FakeMediaDirectory rootDir = new("");

		rootDir.AddFile(new FakeMediaFile("r1", "puid-r1", "root.jpg", "/root.jpg", 100));
		rootDir.AddFile(new FakeMediaFile("r2", "puid-r2", "notes.txt", "/notes.txt", 50));

		FakeMediaDirectory subDir1 = new("SubDir1");
		subDir1.AddFile(new FakeMediaFile("s1", "puid-s1", "sub1.jpg", "/SubDir1/sub1.jpg", 200));
		subDir1.AddFile(new FakeMediaFile("s2", "puid-s2", "readme.txt", "/SubDir1/readme.txt", 30));

		FakeMediaDirectory nestedDir = new("NestedDir");
		nestedDir.AddFile(new FakeMediaFile("n1", "puid-n1", "nested.txt", "/SubDir1/NestedDir/nested.txt", 10));
		subDir1.AddDirectory(nestedDir);

		rootDir.AddDirectory(subDir1);

		FakeMediaDirectory emptyDir = new("EmptyDir");
		rootDir.AddDirectory(emptyDir);

		FakeMediaDrive drive = new("Drive", rootDir);
		FakeMediaDevice device = new("Device");
		FakeConnectedMediaDriveSource source = new(device, drive);

		return (new MediaDeviceTraversal(source, new FakeGatekeeper()), rootDir);
	}
}

// ==================================================
// Fakes — internal, used only by these tests
// ==================================================

internal sealed class FakeMediaFile : IMediaFile
{
	public string Id { get; }
	public string PersistentUniqueId { get; }
	public string Name { get; }
	public string FullName { get; }
	public ulong Length { get; }
	public DateTime? CreationTime { get; }
	public DateTime? LastWriteTime { get; }
	public DateTime? DateAuthored { get; }
	public MediaFileAttribute Attributes { get; }

	public FakeMediaFile(
		string id, string persistentUniqueId,
		string name, string fullName, ulong length,
		DateTime? created = null, DateTime? modified = null, DateTime? authored = null)
	{
		Id = id;
		PersistentUniqueId = persistentUniqueId;
		Name = name;
		FullName = fullName;
		Length = length;
		CreationTime = created;
		LastWriteTime = modified;
		DateAuthored = authored;
	}

	public Stream OpenRead() => new MemoryStream();
}

internal sealed class FakeMediaDirectory : IMediaDirectory
{
	public string Name { get; }
	public string FullName => Name;
	public DateTime? CreationTime => null;
	public DateTime? LastWriteTime => null;
	public DateTime? DateAuthored => null;
	public MediaFileAttribute Attributes => MediaFileAttribute.Directory;
	public string Id => Name;
	public string PersistentUniqueId => Name;

	private readonly List<IMediaFile> _files = new();
	private readonly List<IMediaDirectory> _subDirs = new();

	public IReadOnlyList<IMediaFile> Files => _files;
	public IReadOnlyList<IMediaDirectory> Directories => _subDirs;

	public FakeMediaDirectory(string name)
	{
		Name = name;
	}

	public void AddFile(IMediaFile file) => _files.Add(file);
	public void AddDirectory(IMediaDirectory dir) => _subDirs.Add(dir);

	public IEnumerable<IMediaFile> EnumerateFiles() => _files;
	public IEnumerable<IMediaDirectory> EnumerateDirectories() => _subDirs;
}

internal sealed class FakeMediaDrive : IMediaDrive
{
	public string Name { get; }
	public IMediaDirectory? RootDirectory { get; }
	public long AvailableFreeSpace => 0;
	public string DriveFormat => "FAT32";
	public string DriveType => "Removable";
	public bool IsReady => true;
	public long TotalFreeSpace => 0;
	public long TotalSize => 0;
	public string VolumeLabel => "Volume";

	public FakeMediaDrive(string name, IMediaDirectory? rootDirectory)
	{
		Name = name;
		RootDirectory = rootDirectory;
	}
}

internal sealed class FakeMediaDevice : IMediaDevice
{
	public string DeviceId { get; }
	public string FriendlyName { get; }
	public string Description => "Fake Device";
	public string Manufacturer => "FakeCorp";
	public string FirmwareVersion => "1.0";
	public string Protocol => "MTP";
	public string Model => "FakeModel";
	public string SerialNumber => "SN123";
	public string DeviceType => "Portable";
	public byte[]? FunctionalUniqueId => null;
	public byte[]? ModelUniqueId => null;
	public IReadOnlyList<IMediaDrive> Drives => Array.Empty<IMediaDrive>();

	public FakeMediaDevice(string friendlyName, string deviceId = "dev-001")
	{
		FriendlyName = friendlyName;
		DeviceId = deviceId;
	}

	public IMediaDevice Connect() => this;
	public IMediaDeviceInfo Disconnect() => this;
	public void Dispose() { }
}

internal sealed class FakeConnectedMediaDriveSource : IConnectedMediaDriveSource
{
	public IMediaDevice Device { get; }
	public IMediaDrive Drive { get; }
	public string Name => "FakeSource";

	public FakeConnectedMediaDriveSource(IMediaDevice device, IMediaDrive drive)
	{
		Device = device;
		Drive = drive;
	}

	public void Dispose() { }
}

// ==================================================
// Synchronous IProgress<T> — reports inline for test reliability
// ==================================================

internal sealed class TestProgress<T> : IProgress<T>
{
	private readonly List<T> _reports;

	public TestProgress(List<T> reports)
	{
		_reports = reports;
	}

	public void Report(T value) => _reports.Add(value);
}

// ==================================================
// Accessor — uses reflection to invoke private methods
// ==================================================

internal static class MediaDeviceTraversal_Accessor
{
	private static readonly Type TraversalType = typeof(MediaDeviceTraversal);

	public static string GenerateDeviceUniqueId(
		string fullFilePath, ulong size,
		DateTimeOffset? dateCreated, DateTimeOffset? dateModified, DateTimeOffset? dateAuthored)
		=> MediaDeviceTraversal.GenerateDeviceUniqueId(fullFilePath, size, dateCreated, dateModified, dateAuthored);

	public static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
		=> InvokeStatic<DateTimeOffset?>(nameof(ToUtcOffsetOrNull), value);

	private static T InvokeStatic<T>(string name, params object?[] args)
	{
		var method = TraversalType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException($"Private static method '{name}' not found on {TraversalType.Name}.");
		try
		{
			return (T)method.Invoke(null, args)!;
		} catch(TargetInvocationException ex)
		{
			throw ex.InnerException ?? ex;
		}
	}
}
