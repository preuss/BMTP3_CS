using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.Strategies;

public class CollisionResolverResolutionTests
{
	private static CollisionResolver BuildResolver()
	{
		return new CollisionResolver(
			new DummyMetadataReader(),
			new SimplePathGenerator(),
			LoggerFactory.Create(_ => { }).CreateLogger<CollisionResolver>(),
			null);
	}

	// Helper: writes two different files to disk, returns (srcPath, destPath).
	private static async Task<(string src, string dest)> CreateDifferentFilesAsync(string dirName)
	{
		string dir = Path.Combine(Path.GetTempPath(), dirName);
		Directory.CreateDirectory(dir);

		string src = Path.Combine(dir, "source.jpg");
		string dest = Path.Combine(dir, "dest.jpg");

		await File.WriteAllBytesAsync(src, new byte[] { 1, 2, 3 });
		await File.WriteAllBytesAsync(dest, new byte[] { 4, 5, 6 });
		return (src, dest);
	}

	private static BackupItem CreateItemFromFile(string srcPath)
	{
		FileContent content = new(srcPath);
		BackupItem item = BackupItem.Create(content, Path.GetFileName(srcPath));
		return item;
	}

	// ──────────────────────────────────────────────────────────
	// Overwrite resolution
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_Overwrite_WhenFilesAreDifferent_ReturnsCopyAction()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_overwrite");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Overwrite
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			Assert.Equal(BackupActionType.Copy, result.Action);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	[Fact]
	public async Task ResolveAsync_Overwrite_WhenFilesAreDifferent_ReasonContainsOverwrite()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_overwrite2");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Overwrite
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			Assert.Contains("Overwrite", result.Reason, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	// ──────────────────────────────────────────────────────────
	// Skip resolution
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_Skip_WhenFilesAreDifferent_ReturnsSkipAction()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_skip");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Skip
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			Assert.Equal(BackupActionType.Skip, result.Action);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	// ──────────────────────────────────────────────────────────
	// Error resolution
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_Error_WhenFilesAreDifferent_ThrowsIOException()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_error");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Error
			};

			await Assert.ThrowsAsync<IOException>(() =>
				BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None));
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	// ──────────────────────────────────────────────────────────
	// Timestamp rename strategy
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_Rename_TimestampStrategy_ProducesPathWithTimestampSuffix()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_ts");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			DateTime authored = new(2024, 6, 15, 8, 30, 45, DateTimeKind.Utc);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Rename,
				RenameStrategy = RenameStrategy.Timestamp
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			// Expected suffix: _20240615_083045
			Assert.Contains("_20240615_083045", result.TargetPath);
			Assert.Equal(BackupActionType.Rename, result.Action);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	// ──────────────────────────────────────────────────────────
	// Increment rename strategy
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_Rename_IncrementStrategy_AppendsUnderscoreOne()
	{
		(string src, string dest) = await CreateDifferentFilesAsync("bmtp3_cr_incr");
		try
		{
			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Rename,
				RenameStrategy = RenameStrategy.Increment
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			// The generated path should not exist and should contain _1
			string expectedName = Path.GetFileNameWithoutExtension(dest) + "_1" + Path.GetExtension(dest);
			Assert.Equal(expectedName, Path.GetFileName(result.TargetPath));
			Assert.Equal(BackupActionType.Rename, result.Action);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(Path.GetDirectoryName(dest)!);
		}
	}

	[Fact]
	public async Task ResolveAsync_Rename_IncrementStrategy_ParallelCalls_ReturnUniqueTargets()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_parallel");
		Directory.CreateDirectory(dir);

		string dest = Path.Combine(dir, "existing.jpg");
		await File.WriteAllBytesAsync(dest, new byte[] { 99, 98, 97 });

		BackupPlan plan = new()
		{
			ComparisonType = CollisionComparisonType.Binary,
			CollisionResolution = CollisionResolutionType.Rename,
			RenameStrategy = RenameStrategy.Increment
		};

		const int workerCount = 8;
		CollisionResolver resolver = BuildResolver();
		List<Task<CollisionResult>> tasks = new();
		List<string> sourceFiles = new();

		try
		{
			for (int i = 0; i < workerCount; i++)
			{
				string src = Path.Combine(dir, $"source_{i}.jpg");
				await File.WriteAllBytesAsync(src, new[] { (byte)i, (byte)(i + 1), (byte)(i + 2) });
				sourceFiles.Add(src);

				BackupItem item = CreateItemFromFile(src);
				tasks.Add(resolver.ResolveAsync(item, dest, plan, CancellationToken.None));
			}

			CollisionResult[] results = await Task.WhenAll(tasks);
			List<string> renamedTargets = results.Select(r => r.TargetPath).ToList();

			Assert.All(results, r => Assert.Equal(BackupActionType.Rename, r.Action));
			Assert.Equal(workerCount, renamedTargets.Distinct(StringComparer.OrdinalIgnoreCase).Count());
			Assert.False(renamedTargets.Any(p => string.Equals(p, dest, StringComparison.OrdinalIgnoreCase)));
		}
		finally
		{
			foreach (string src in sourceFiles)
			{
				TryDelete(src);
			}

			TryDelete(dest);
			TryDeleteDir(dir);
		}
	}

	// ──────────────────────────────────────────────────────────
	// ComparisonType.None — always treats files as different
	// ──────────────────────────────────────────────────────────

	[Fact]
	public async Task ResolveAsync_ComparisonTypeNone_WhenDestinationExists_TreatsAsCollision()
	{
		// Write two *identical* files – with None comparison they should still be treated as different
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_none");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "same_src.jpg");
		string dest = Path.Combine(dir, "same_dst.jpg");
		try
		{
			byte[] data = new byte[] { 10, 20, 30 };
			await File.WriteAllBytesAsync(src, data);
			await File.WriteAllBytesAsync(dest, data);

			BackupItem item = CreateItemFromFile(src);
			BackupPlan plan = new()
			{
				ComparisonType = CollisionComparisonType.None,
				CollisionResolution = CollisionResolutionType.Rename,
				RenameStrategy = RenameStrategy.Increment
			};

			CollisionResult result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

			// ComparisonType.None skips content check -> files treated as different -> Rename applied
			Assert.Equal(BackupActionType.Rename, result.Action);
		}
		finally
		{
			TryDelete(src);
			TryDelete(dest);
			TryDeleteDir(dir);
		}
	}

	// ──────────────────────────────────────────────────────────
	// Cleanup helpers
	// ──────────────────────────────────────────────────────────

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
		}
	}

	private static void TryDeleteDir(string dir)
	{
		try
		{
			Directory.Delete(dir);
		}
		catch
		{
		}
	}
	// ──────────────────────────────────────────────────────────
	// Shared test doubles (mirrors pattern from existing tests)
	// ──────────────────────────────────────────────────────────

	private class SimplePathGenerator : IPathGenerator
	{
		public string ApplyPattern(string pattern, IBackupItem item)
		{
			return Path.Combine(Path.GetDirectoryName(item.SourcePath) ?? string.Empty,
				Path.GetFileName(item.SourcePath));
		}

		public string GenerateRelativePath(IBackupItem item, BackupPlan plan)
		{
			return Path.GetFileName(item.SourcePath);
		}
	}

	private class DummyMetadataReader : IMetadataReader
	{
		public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
		{
			return Task.CompletedTask;
		}
	}
}