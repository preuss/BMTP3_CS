

namespace BMTP3.Core4.Models;
/// <summary>
///     Abstraction for accessing the content of a source file, regardless of whether it is
///     on a local file system or a portable device (MTP).
/// </summary>
internal interface IContent
{
	/// <summary>
	///     Size of the content in bytes. Used for progress reporting and buffer allocation.
	/// </summary>
	ulong Length { get; }

	/// <summary>
	///     Opens a new readable stream to the content.
	///     The caller is responsible for disposing the returned stream.
	/// </summary>
	Stream OpenRead();

	/// <summary>
	///     Opens a new readable stream to the content asynchronously.
	///     The caller is responsible for disposing the returned stream.
	/// </summary>
	Task<Stream> OpenReadAsync(CancellationToken ct);
}
