using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;
internal sealed class DownloadService : IDownloadService
{
	private const int BufferSize = 80 * 1024; // 80 KB buffer size for efficient copying
	public async Task<IMoveableContent> DownloadAsync(
		FileInfo destination,
		IContent source,
		IProgress<ulong>? totalBytesReadProgress,
		CancellationToken cancellationToken
	)
	{
		await using Stream sourceStream = await source.OpenReadStreamAsync(cancellationToken);
		await using FileStream destStream = destination.Create();
		ulong totalBytesRead = 0;
		byte[] buffer = new byte[BufferSize];
		int read;
		while((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
		{
			await destStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
			totalBytesRead += (ulong)read;
			totalBytesReadProgress?.Report((ulong)totalBytesRead);
		}
		return new MoveableFileContent(destination.FullName);
	}
}