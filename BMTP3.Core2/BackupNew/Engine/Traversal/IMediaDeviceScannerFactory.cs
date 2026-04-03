using MediaDevices;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
/// Creates an <see cref="ITraversalScanner{MediaFileInfo}"/> bound to a specific, already-connected
/// <see cref="MediaDevice"/>. The factory keeps device lifecycle management in <see cref="BackupScanner"/>
/// while satisfying the Dependency Inversion Principle for the scanner construction.
/// </summary>
public interface IMediaDeviceScannerFactory
{
	/// <summary>
	/// Returns a scanner that will enumerate files on the given <paramref name="connectedDevice"/>.
	/// The caller is responsible for keeping the device connected for the lifetime of the scanner.
	/// </summary>
	ITraversalScanner<MediaFileInfo> Create(MediaDevice connectedDevice);
}
