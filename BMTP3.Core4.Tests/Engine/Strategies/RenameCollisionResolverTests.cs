using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Compare;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Infrastructure.Throttling;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Strategies;

public class RenameCollisionResolverTests
{
	private static readonly string NonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
	private const string NonExistentFile = @"C:\__nope__\file.txt";

	private static readonly RenameCollisionRequest BaseRequest = new(
		SourcePath: NonExistentFile,
		IntendedTargetPath: NonExistentFile,
		RelativeFilePath: "file.txt",
		FileName: "file.txt",
		CreateFileDate: new DateTimeOffset(2026, 6, 1, 14, 30, 22, TimeSpan.Zero),
		StrongHash: "abcdef123456",
		ItemId: "item-1",
		RenameStrategy: RenameStrategy.Increment,
		CustomRenamePattern: null,
		ComparisonType: CollisionComparisonType.None,
		ComparisonHashAlgorithmTypes: Array.Empty<HashAlgorithmType>(),
		ComputedHashes: null,
		DeviceName: null,
		DeviceModel: null
	);

	private static RenameCollisionResolver CreateResolver(
		IFileCompareService? fileCompare = null,
		IHashService? hashService = null,
		IMessageFormatter? formatter = null,
		IFileFormatValuesFactory? formatValuesFactory = null)
	{
		return new RenameCollisionResolver(
			fileCompare ?? new FakeFileCompareService(),
			hashService ?? new FakeHashService(),
			formatter ?? new FakeMessageFormatter(),
			formatValuesFactory ?? new FakeFileFormatValuesFactory()
		);
	}

	private static string GetUniqueNonExistentPath() =>
		Path.Combine(NonExistentDir, Guid.NewGuid().ToString() + ".txt");

	[Fact]
	public async Task Increment_GeneratesNumberedPath()
	{
		RenameCollisionResolver resolver = CreateResolver();
		string path = GetUniqueNonExistentPath();
		RenameCollisionRequest request = BaseRequest with
		{
			IntendedTargetPath = path,
			SourcePath = path,
			RenameStrategy = RenameStrategy.Increment,
		};

		RenameCollisionResult result = await resolver.ResolveAsync(request, null, default);

		Assert.Equal(CollisionResolutionAction.Move, result.Action);
		Assert.Matches(@".+_1\.txt$", result.TargetPath);
	}

	[Fact]
	public async Task Timestamp_GeneratesTimestampedPath()
	{
		RenameCollisionResolver resolver = CreateResolver();
		string path = GetUniqueNonExistentPath();
		RenameCollisionRequest request = BaseRequest with
		{
			IntendedTargetPath = path,
			SourcePath = path,
			RenameStrategy = RenameStrategy.Timestamp,
		};

		RenameCollisionResult result = await resolver.ResolveAsync(request, null, default);

		Assert.Equal(CollisionResolutionAction.Move, result.Action);
		Assert.Contains("20260601_143022", result.TargetPath);
	}

	[Fact]
	public async Task Hash_GeneratesHashPrefixedPath()
	{
		RenameCollisionResolver resolver = CreateResolver();
		string path = GetUniqueNonExistentPath();
		RenameCollisionRequest request = BaseRequest with
		{
			IntendedTargetPath = path,
			SourcePath = path,
			RenameStrategy = RenameStrategy.Hash,
		};

		RenameCollisionResult result = await resolver.ResolveAsync(request, null, default);

		Assert.Equal(CollisionResolutionAction.Move, result.Action);
		Assert.Contains("abcdef", result.TargetPath);
	}

	[Fact]
	public async Task Hash_NoStrongHash_Throws()
	{
		RenameCollisionResolver resolver = CreateResolver();
		string path = GetUniqueNonExistentPath();
		RenameCollisionRequest request = BaseRequest with
		{
			IntendedTargetPath = path,
			SourcePath = path,
			RenameStrategy = RenameStrategy.Hash,
			StrongHash = null,
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			resolver.ResolveAsync(request, null, default));
	}

	[Fact]
	public async Task Custom_UsesMessageFormatter()
	{
		RenameCollisionResolver resolver = CreateResolver();
		string dir = GetUniqueNonExistentPath();
		string intendedPath = Path.Combine(dir, "photo.jpg");

		RenameCollisionRequest request = BaseRequest with
		{
			SourcePath = intendedPath,
			IntendedTargetPath = intendedPath,
			FileName = "photo.jpg",
			RelativeFilePath = "photo.jpg",
			RenameStrategy = RenameStrategy.Custom,
			CustomRenamePattern = "{fileName}_{count}",
		};

		RenameCollisionResult result = await resolver.ResolveAsync(request, null, default);

		Assert.Equal(CollisionResolutionAction.Move, result.Action);
		int endOfDir = dir.Length;
		string relativeResult = result.TargetPath.Substring(endOfDir).TrimStart('\\', '/');
		Assert.Equal("photo_1", relativeResult);
	}

	[Fact]
	public async Task Custom_NoPattern_Throws()
	{
		RenameCollisionResolver resolver = CreateResolver();
		RenameCollisionRequest request = BaseRequest with
		{
			RenameStrategy = RenameStrategy.Custom,
			CustomRenamePattern = null,
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			resolver.ResolveAsync(request, null, default));
	}
}
