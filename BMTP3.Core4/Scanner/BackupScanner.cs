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

		SourceTraversalRequest traversalRequest = new()
		{
			SourcePath = request.SourcePath,
			SubPath = request.SubPath,
			Recursive = request.Recursive,
			IncludePatterns = request.IncludePatterns,
			ExcludePatterns = request.ExcludePatterns,
			ItemIdScope = request.ItemIdScope,
		};

		IProgress<SourceTraversalProgress>? traversalProgress = progress is not null
			? new Progress<SourceTraversalProgress>(tp => progress.Report(new BackupScanProgress
			{
				DirectoriesTraversed = tp.DirectoriesTraversed,
				FilesDiscovered = tp.FilesDiscovered,
			}))
			: null;

		await foreach(SourceTraversalItem sourceItem in traversal.TraverseAsync(traversalRequest, traversalProgress, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			BackupItem item = new(sourceItem.Content)
			{
				Id = sourceItem.Id,
				SourcePath = sourceItem.SourcePath,
				RelativeFilePath = sourceItem.RelativeFilePath,
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
