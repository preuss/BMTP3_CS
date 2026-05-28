using BMTP3.Core4.Engine.Helpers;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;
internal sealed class DownloadService : IDownloadService
{
	private const int BufferSize = 80 * 1024; // 80 KB buffer size for efficient copying
	public async Task<IMoveableContent> DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken)
	{
		await using Stream sourceStream = await request.Source.OpenReadStreamAsync(cancellationToken);
		await using FileStream destStream = request.Destination.Create();
		ulong totalBytesRead = 0;
		byte[] buffer = new byte[BufferSize];
		int read;
		while((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
		{
			await destStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
			totalBytesRead += (ulong)read;
			progress?.Report((ulong)totalBytesRead);
		}

		DateTime sourceLocal = TimestampHelpers.FindEarliestValidDate(
			request.DateAuthored, request.DateCreated, request.DateModified, request.DateAccessed).LocalDateTime;

		request.Destination.CreationTime = sourceLocal;
		request.Destination.LastAccessTime = sourceLocal;
		request.Destination.LastWriteTime = sourceLocal;

		return new MoveableFileContent(request.Destination.FullName);
	}
}