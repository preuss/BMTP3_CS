	using BMTP3.Core4.Engine.Helpers;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;
internal sealed class DownloadService : IDownloadService
{
	private const int BufferSize = 80 * 1024;

	public async Task DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken)
	{
		await using Stream sourceStream = await request.Item.Content.OpenReadStreamAsync(cancellationToken);
		await using FileStream destStream = request.Destination.Create();
		ulong totalBytesRead = 0;
		byte[] buffer = new byte[BufferSize];
		int read;
		while((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
		{
			await destStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
			totalBytesRead += (ulong)read;
			progress?.Report(totalBytesRead);
		}

		DateTimeOffset appliedDate = TimestampHelpers.FindEarliestValidDate(
			request.Item.DateAuthored,
			request.Item.DateCreated,
			request.Item.DateModified,
			request.Item.DateAccessed,
			request.BackupStartTime
		);
		DateTime sourceLocal = appliedDate.LocalDateTime;

		request.Destination.CreationTime = sourceLocal;
		request.Destination.LastAccessTime = sourceLocal;
		request.Destination.LastWriteTime = sourceLocal;

		request.Item.DateCreated = appliedDate;
		request.Item.DateModified = appliedDate;
		request.Item.DateAccessed = appliedDate;

		request.Item.ReplaceContentProvider(new MoveableFileContent(request.Destination.FullName));
	}
}