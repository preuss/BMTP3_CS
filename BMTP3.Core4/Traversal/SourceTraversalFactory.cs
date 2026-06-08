using BMTP3.Core4.Storage;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Traversal;

/// <summary>
///     Default implementation of <see cref="ISourceTraversalFactory" />.
///     Inspects the runtime type of <see cref="IConnectedSource" /> to select
///     the correct <see cref="ISourceTraversal" /> strategy.
/// </summary>
internal sealed class SourceTraversalFactory : ISourceTraversalFactory
{
	private readonly IMediaDeviceGatekeeper _gatekeeper;

	public SourceTraversalFactory(IMediaDeviceGatekeeper gatekeeper)
	{
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	/// <summary>
	///     Creates an <see cref="ISourceTraversal" /> by pattern-matching on
	///     the connected source type.
	/// </summary>
	/// <param name="connectedSource">
	///     The connected source. Must implement <see cref="IConnectedFileSystemSource" />
	///     or <see cref="IConnectedMediaDriveSource" />.
	/// </param>
	/// <returns>
	///     A <see cref="FileSystemTraversal" /> for file-system sources,
	///     or a <see cref="MediaDeviceTraversal" /> for MTP devices.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">
	///     Thrown when <paramref name="connectedSource" /> is not a recognised type.
	/// </exception>
	public ISourceTraversal Create(IConnectedSource connectedSource)
	{
		ArgumentNullException.ThrowIfNull(connectedSource);

		return connectedSource switch
		{
			IConnectedFileSystemSource fileSystemSource => CreateFileSystemTraversal(fileSystemSource),

#pragma warning disable CA1416
			IConnectedMediaDriveSource mediaDeviceSource => CreateMediaDeviceTraversal(mediaDeviceSource),
#pragma warning restore CA1416

			_ => throw new ArgumentOutOfRangeException(nameof(connectedSource), $"Unknown connected source type: {connectedSource.GetType().Name}")
		};
	}

	private static ISourceTraversal CreateFileSystemTraversal(IConnectedFileSystemSource connectedSource)
	{
		return new FileSystemTraversal();
	}


	[SupportedOSPlatform("windows7.0")]
	private MediaDeviceTraversal CreateMediaDeviceTraversal(IConnectedMediaDriveSource mediaDriveSource)
	{
		return new MediaDeviceTraversal(mediaDriveSource, _gatekeeper);

	}
}
