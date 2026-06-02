using BMTP3.Core4.Engine.Sidecar.Document;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal interface ISidecarWriter
{
	Task WriteToFileAsync(SidecarDocument document, string filePath, CancellationToken cancellationToken);
}
