namespace BMTP3.Core4.Api;

/// <summary>
/// Represents progress information for a file that is currently active
/// in the backup workflow.
/// </summary>
public interface IFileProgress
{
	/// <summary>
	/// The source-relative path of the file.
	/// </summary>
	string Path { get; }

	/// <summary>
	/// The size of the file in bytes, if known.
	/// </summary>
	long? BytesTotal { get; }

	/// <summary>
	/// The number of bytes processed for this file so far.
	/// </summary>
	long BytesProcessed { get; }
}