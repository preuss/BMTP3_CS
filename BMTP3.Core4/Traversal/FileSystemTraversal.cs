using BMTP3.Common.Utilities;
using BMTP3.Core4.Helpers;
using System.Runtime.CompilerServices;
using BMTP3.Core4.Api.Models.Enums;

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

		IReadOnlyList<string>? includePatterns = request.IncludePatterns;
		IReadOnlyList<string>? excludePatterns = request.ExcludePatterns;

		string externalSourcePath = PathHelper.FromCanonicalFileUri(request.SourcePath);
		DirectoryInfo sourceDirectory = new(externalSourcePath);

		if(!sourceDirectory.Exists)
			throw new DirectoryNotFoundException($"Source path does not exist: {request.SourcePath}");

		int dirCount = 0;
		int fileCount = 0;

		await foreach(FileInfo file in EnumerateFilesRecursiveAsync(sourceDirectory, request.Recursive, () => dirCount++, cancellationToken))
		{
			fileCount++;
			progress?.Report(new SourceTraversalProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			string internalFullFileName = PathHelper.ToInternalCanonicalUri(file.FullName, BackupSourceType.FileSystem);
			string internalRelativeFilePath = PathHelper.GetRelativePath(request.SourcePath, internalFullFileName);
			if(!GlobMatcher.IsIncluded(internalRelativeFilePath, includePatterns, excludePatterns))
				continue;

			DateTimeOffset? created = SafeGetDate(file, f => f.CreationTimeUtc);
			DateTimeOffset? modified = SafeGetDate(file, f => f.LastWriteTimeUtc);
			DateTimeOffset? accessed = SafeGetDate(file, f => f.LastAccessTimeUtc);

			yield return new SourceTraversalItem
			{
				Id = internalFullFileName,
				SourcePath = internalFullFileName,
				RelativeFilePath = internalRelativeFilePath,
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
        return dir.EnumerateFiles();
    }

    private static IEnumerable<DirectoryInfo> SafeGetDirectories(DirectoryInfo dir)
    {
        return dir.EnumerateDirectories();
    }

	/// <summary>
	/// Only supports getting UTC dates, and returns null if any exception occurs (e.g. due to permissions).
	/// </summary>
	/// <param name="file"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	private static DateTimeOffset? SafeGetDate(FileInfo file, Func<FileInfo, DateTime> selector)
    {
        try
        {
            return new DateTimeOffset(selector(file), TimeSpan.Zero);
        } catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or NotSupportedException)
        {
            return null;
        }
    }
}