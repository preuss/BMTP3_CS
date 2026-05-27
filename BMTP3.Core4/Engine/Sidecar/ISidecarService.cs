namespace BMTP3.Core4.Engine.Sidecar;

internal interface ISidecarService
{
	Task WriteAsync(string targetFilePath, SidecarRequest request, CancellationToken cancellationToken);
}
