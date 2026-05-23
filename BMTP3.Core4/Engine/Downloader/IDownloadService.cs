using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;
internal interface IDownloadService
{
	Task<IMoveableContent> DownloadAsync(
		FileInfo destination,
		IContent source,
		IProgress<ulong>? totalBytesReadProgress,
		CancellationToken cancellationToken
	);
}
