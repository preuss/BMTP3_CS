using BMTP3.Core4.Engine.Sidecar.Document;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal interface ISidecarWriter
{
	Task WriteToStreamAsync(SidecarDocument document, Stream stream, CancellationToken cancellationToken);
}
