using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Downloader;

internal sealed record DownloadRequest
{
	public required FileInfo Destination { get; init; }
	public required IContent Source { get; init; }
	public DateTimeOffset? DateAuthored { get; init; }
	public DateTimeOffset? DateCreated { get; init; }
	public DateTimeOffset? DateModified { get; init; }
	public DateTimeOffset? DateAccessed { get; init; }
}
