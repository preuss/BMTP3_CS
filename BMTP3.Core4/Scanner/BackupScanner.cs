using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;
using System.Runtime.CompilerServices;

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
			SubPath = request.SubPath,
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

			BackupItem item = new(sourceItem.Content)
			{
				Id = sourceItem.Id,
				SourcePath = sourceItem.SourcePath,
				RelativePath = sourceItem.RelativePath,
				FileName = sourceItem.FileName,
				DateCreated = sourceItem.DateCreated,
				DateModified = sourceItem.DateModified,
				DateAuthored = sourceItem.DateAuthored,
				DateAccessed = sourceItem.DateAccessed,
			};

			yield return item;
		}
	}
}
