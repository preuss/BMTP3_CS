using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;

internal sealed record DownloadRequest
{
	public required FileInfo Destination { get; init; }
	public required BackupItem Item { get; init; }
	public required DateTimeOffset BackupStartTime { get; init; }
}
