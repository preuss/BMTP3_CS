using BMTP3.Core4.Utilities;
using System.Runtime.CompilerServices;

namespace BMTP3.Core4.Traversal;

internal sealed class FileSystemTraversal : ISourceTraversal
{
	public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(request);

		DirectoryInfo rootDir = new(request.SourcePath);

		if(!rootDir.Exists)
			throw new DirectoryNotFoundException($"Source path does not exist: {request.SourcePath}");

		int dirCount = 0;
		int fileCount = 0;

		await foreach(FileInfo file in EnumerateFilesRecursiveAsync(rootDir, request.Recursive, () => dirCount++, cancellationToken))
		{
			fileCount++;
			progress?.Report(new SourceTraversalProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			DateTimeOffset? created = SafeGetDate(file, f => f.CreationTimeUtc);
			DateTimeOffset? modified = SafeGetDate(file, f => f.LastWriteTimeUtc);
			DateTimeOffset? accessed = SafeGetDate(file, f => f.LastAccessTimeUtc);

			string relativeFilePath = Path.GetRelativePath(rootDir.FullName, file.FullName);
			if(!GlobMatcher.IsIncluded(relativeFilePath, request.IncludePatterns, request.ExcludePatterns))
				continue;

			yield return new SourceTraversalItem
			{
				Id = file.FullName,
				SourcePath = file.FullName,
				RelativeFilePath = relativeFilePath,
				FileName = file.Name,
				Content = new Models.FileContent(file),
				DateCreated = created,
				DateModified = modified,
				DateAccessed = accessed,
			};
		}
	}

	private static async IAsyncEnumerable<FileInfo> EnumerateFilesRecursiveAsync(
		DirectoryInfo dir,
		bool recursive,
		Action? onDirectoryEntered,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		foreach(FileInfo file in SafeGetFiles(dir))
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return file;
		}

		if(!recursive)
			yield break;

		foreach(DirectoryInfo subDir in SafeGetDirectories(dir))
		{
			cancellationToken.ThrowIfCancellationRequested();
			onDirectoryEntered?.Invoke();

			await foreach(FileInfo file in EnumerateFilesRecursiveAsync(subDir, recursive, onDirectoryEntered, cancellationToken))
			{
				yield return file;
			}
		}
	}

	private static IEnumerable<FileInfo> SafeGetFiles(DirectoryInfo dir)
	{
		try
		{
			return dir.EnumerateFiles();
		} catch
		{
			return Array.Empty<FileInfo>();
		}
	}

	private static IEnumerable<DirectoryInfo> SafeGetDirectories(DirectoryInfo dir)
	{
		try
		{
			return dir.EnumerateDirectories();
		} catch
		{
			return Array.Empty<DirectoryInfo>();
		}
	}

	private static DateTimeOffset? SafeGetDate(FileInfo file, Func<FileInfo, DateTime> selector)
	{
		try
		{
			return new DateTimeOffset(selector(file), TimeSpan.Zero);
		} catch
		{
			return null;
		}
	}
}
