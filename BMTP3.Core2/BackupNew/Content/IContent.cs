namespace BMTP3.Core2.BackupNew.Content;

/// <summary>
/// Abstraction for accessing the content of a source file, regardless of whether it is 
/// on a local file system or a portable device (MTP).
/// </summary>
public interface IContent : IDisposable
{
	/// <summary>
	/// Size of the content in bytes. Used for progress reporting and buffer allocation.
	/// </summary>
	ulong Length { get; }

	/// <summary>
	/// Opens a new readable stream to the content.
	/// The caller is responsible for disposing the returned stream.
	/// </summary>
	Stream OpenRead();
}
