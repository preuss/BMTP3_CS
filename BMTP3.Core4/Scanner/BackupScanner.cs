using System.Runtime.CompilerServices;
using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Scanner;

internal sealed class BackupScanner : IBackupScanner
{
	public async IAsyncEnumerable<BackupItem> ScanAsync(
		ISourceTraversal traversal,
		BackupScanRequest request,
		IProgress<BackupScanProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(traversal);
		ArgumentNullException.ThrowIfNull(request);

		int dirCount = 0;
		int fileCount = 0;

		SourceTraversalRequest traversalRequest = new()
		{
			SourcePath = request.SourcePath,
			Recursive = request.Recursive,
			IncludePatterns = request.IncludePatterns,
			ExcludePatterns = request.ExcludePatterns,
		};

		await foreach(SourceTraversalItem sourceItem in traversal.TraverseAsync(traversalRequest, null, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			fileCount++;
			progress?.Report(new BackupScanProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			string relativePath = Path.GetRelativePath(request.SourcePath, sourceItem.SourcePath);
			string fileName = Path.GetFileName(sourceItem.SourcePath);

			BackupItem item = new(sourceItem.Content)
			{
				Id = relativePath,
				SourcePath = sourceItem.SourcePath,
				RelativePath = relativePath,
				FileName = fileName,
				DateCreated = sourceItem.DateCreated,
				DateModified = sourceItem.DateModified,
				DateAuthored = sourceItem.DateAuthored,
				DateAccessed = sourceItem.DateAccessed,
			};

			yield return item;
		}
	}
}
