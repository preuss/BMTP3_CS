using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Traversal;

internal sealed class SourceTraversalFactory : ISourceTraversalFactory
{
	public ISourceTraversal Create(SourceTraversalFactoryCreateRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		return request.SourceType switch
		{
			BackupSourceType.FileSystem => new FileSystemTraversal(),
			BackupSourceType.MediaDevice => throw new NotSupportedException(
				$"Source type '{request.SourceType}' is not supported by this factory. Use a MediaDeviceTraversalFactory for MTP devices."),
			_ => throw new ArgumentOutOfRangeException(nameof(request.SourceType),
				$"Unknown source type: {request.SourceType}"),
		};
	}
}
