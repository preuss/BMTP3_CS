using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;
internal interface IDownloadService
{
	Task<IMoveableContent> DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken);
}
