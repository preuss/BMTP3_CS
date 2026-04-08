using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Core2.Tests.Traversal;

public class BackupScannerTests
{
	/// <summary>
	///     Builds a real <see cref="IBackupScanner" /> via the production DI registration.
	/// </summary>
	private static IBackupScanner BuildScanner()
	{
		ServiceCollection services = new();
		services.AddLogging();
		services.AddBMTP3Core2();
		ServiceProvider sp = services.BuildServiceProvider();
		return sp.GetRequiredService<IBackupScanner>();
	}

	/// <summary>
	///     Creates a temporary directory with a unique name and returns its path.
	/// </summary>
	private static string CreateTempDir()
	{
		string path = Path.Combine(Path.GetTempPath(), $"bmtp3_scan_{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static BackupPlan FilesystemPlan(string sourcePath, bool recursive = false)
	{
		return new BackupPlan
		{
			Name = "Test",
			SourceType = SourceType.FileSystem,
			SourcePath = sourcePath,
			Recursive = recursive
			// No glob filters = include everything
		};
	}

	// -----------------------------------------------------------------------

	[Fact]
	public async Task ScanAsync_FileSystem_YieldsAllFilesInDirectory()
	{
		string tempDir = CreateTempDir();
		try
		{
			// Create 3 files
			string[] files =
			{
				Path.Combine(tempDir, "alpha.txt"),
				Path.Combine(tempDir, "beta.txt"),
				Path.Combine(tempDir, "gamma.jpg")
			};
			foreach (string f in files)
			{
				await File.WriteAllTextAsync(f, "content");
			}

			IBackupScanner scanner = BuildScanner();
			BackupPlan plan = FilesystemPlan(tempDir);

			List<IBackupItem> items = new();
			await foreach (IBackupItem item in scanner.ScanAsync(plan, CancellationToken.None))
			{
				items.Add(item);
			}

			Assert.Equal(3, items.Count);

			// All source paths must correspond to files we created
			string[] scannedNames = items.Select(i => Path.GetFileName(i.SourcePath)).OrderBy(x => x).ToArray();
			Assert.Equal(new[] { "alpha.txt", "beta.txt", "gamma.jpg" }, scannedNames);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ScanAsync_WithIncludeGlob_FiltersToMatchingFiles()
	{
		string tempDir = CreateTempDir();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(tempDir, "photo.jpg"), "jpg");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "notes.txt"), "txt");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "image.jpg"), "jpg2");

			IBackupScanner scanner = BuildScanner();
			BackupPlan plan = FilesystemPlan(tempDir);
			plan.IncludePatterns.Add("**/*.jpg");

			List<IBackupItem> items = new();
			await foreach (IBackupItem item in scanner.ScanAsync(plan, CancellationToken.None))
			{
				items.Add(item);
			}

			Assert.Equal(2, items.Count);
			Assert.All(items, item =>
				Assert.EndsWith(".jpg", item.SourcePath, StringComparison.OrdinalIgnoreCase));
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ScanAsync_WithExcludeGlob_ExcludesMatchingFiles()
	{
		string tempDir = CreateTempDir();
		try
		{
			await File.WriteAllTextAsync(Path.Combine(tempDir, "keep.jpg"), "jpg");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "skip.txt"), "txt");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "also_skip.txt"), "txt2");

			IBackupScanner scanner = BuildScanner();
			BackupPlan plan = FilesystemPlan(tempDir);
			plan.ExcludePatterns.Add("**/*.txt");

			List<IBackupItem> items = new();
			await foreach (IBackupItem item in scanner.ScanAsync(plan, CancellationToken.None))
			{
				items.Add(item);
			}

			Assert.Single(items);
			Assert.EndsWith(".jpg", items[0].SourcePath, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ScanAsync_EmptyDirectory_YieldsNothing()
	{
		string tempDir = CreateTempDir();
		try
		{
			// No files created – directory is empty
			IBackupScanner scanner = BuildScanner();
			BackupPlan plan = FilesystemPlan(tempDir);

			List<IBackupItem> items = new();
			await foreach (IBackupItem item in scanner.ScanAsync(plan, CancellationToken.None))
			{
				items.Add(item);
			}

			Assert.Empty(items);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ScanAsync_WithRecursive_IncludesSubdirectoryFiles()
	{
		string tempDir = CreateTempDir();
		try
		{
			// Root-level file
			await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "root");

			// Sub-directory with two files
			string subDir = Path.Combine(tempDir, "sub");
			Directory.CreateDirectory(subDir);
			await File.WriteAllTextAsync(Path.Combine(subDir, "child1.txt"), "c1");
			await File.WriteAllTextAsync(Path.Combine(subDir, "child2.jpg"), "c2");

			IBackupScanner scanner = BuildScanner();
			BackupPlan plan = FilesystemPlan(tempDir, true);

			List<IBackupItem> items = new();
			await foreach (IBackupItem item in scanner.ScanAsync(plan, CancellationToken.None))
			{
				items.Add(item);
			}

			Assert.Equal(3, items.Count);

			string[] names = items.Select(i => Path.GetFileName(i.SourcePath)).OrderBy(x => x).ToArray();
			Assert.Contains("root.txt", names);
			Assert.Contains("child1.txt", names);
			Assert.Contains("child2.jpg", names);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}
}