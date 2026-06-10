using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Strategies;

public class CollisionResolverTests
{
	private static readonly CollisionResolveRequest BaseRequest = new()
	{
		SourcePath = @"C:\source\file.txt",
		IntendedTargetPath = @"C:\dest\file.txt",
		RelativeFilePath = "file.txt",
		CreateFileDate = DateTimeOffset.UtcNow,
		ItemId = "item-1",
		Strategy = CollisionStrategy.Overwrite,
		ComparisonType = CollisionComparisonType.None,
		RenameStrategy = RenameStrategy.Increment,
		ComparisonHashAlgorithmTypes = Array.Empty<HashAlgorithmType>(),
	};

	[Fact]
	public async Task Overwrite_ReturnsOverwriteAction()
	{
		var resolver = new CollisionResolver(new FakeRenameCollisionResolver());

		CollisionResult result = await resolver.ResolveAsync(
			BaseRequest with { Strategy = CollisionStrategy.Overwrite }, default);

		Assert.Equal(CollisionResolutionAction.Overwrite, result.Action);
		Assert.Equal(@"C:\dest\file.txt", result.TargetPath);
	}

	[Fact]
	public async Task Skip_ReturnsSkipAction()
	{
		var resolver = new CollisionResolver(new FakeRenameCollisionResolver());

		CollisionResult result = await resolver.ResolveAsync(
			BaseRequest with { Strategy = CollisionStrategy.Skip }, default);

		Assert.Equal(CollisionResolutionAction.Skip, result.Action);
	}

	[Fact]
	public async Task Error_ThrowsIOException()
	{
		var resolver = new CollisionResolver(new FakeRenameCollisionResolver());

		IOException ex = await Assert.ThrowsAsync<IOException>(() =>
			resolver.ResolveAsync(BaseRequest with { Strategy = CollisionStrategy.Error }, default));

		Assert.Contains("file.txt", ex.Message);
	}

	[Fact]
	public async Task Rename_DelegatesToRenameResolver()
	{
		bool wasCalled = false;
		var fakeRename = new FakeRenameCollisionResolver((request, _) =>
		{
			wasCalled = true;
			Assert.Equal(@"C:\dest\file.txt", request.IntendedTargetPath);
			Assert.Equal(RenameStrategy.Timestamp, request.RenameStrategy);
			return Task.FromResult(new RenameCollisionResult(CollisionResolutionAction.Move, @"C:\dest\file_1.txt"));
		});
		var resolver = new CollisionResolver(fakeRename);

		CollisionResult result = await resolver.ResolveAsync(
			BaseRequest with
			{
				Strategy = CollisionStrategy.Rename,
				RenameStrategy = RenameStrategy.Timestamp,
			}, default);

		Assert.True(wasCalled);
		Assert.Equal(CollisionResolutionAction.Move, result.Action);
		Assert.Equal(@"C:\dest\file_1.txt", result.TargetPath);
	}

	[Fact]
	public async Task Rename_WhenResolverReturnsSkip_ReturnsSkip()
	{
		var fakeRename = new FakeRenameCollisionResolver((_, _) =>
			Task.FromResult(new RenameCollisionResult(CollisionResolutionAction.Skip, @"C:\dest\file.txt")));
		var resolver = new CollisionResolver(fakeRename);

		CollisionResult result = await resolver.ResolveAsync(
			BaseRequest with { Strategy = CollisionStrategy.Rename }, default);

		Assert.Equal(CollisionResolutionAction.Skip, result.Action);
	}

	[Fact]
	public async Task Rename_WhenResolverReturnsEmptyPath_Throws()
	{
		var fakeRename = new FakeRenameCollisionResolver((_, _) =>
			Task.FromResult(new RenameCollisionResult(CollisionResolutionAction.Move, "")));
		var resolver = new CollisionResolver(fakeRename);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			resolver.ResolveAsync(BaseRequest with { Strategy = CollisionStrategy.Rename }, default));
	}
}
