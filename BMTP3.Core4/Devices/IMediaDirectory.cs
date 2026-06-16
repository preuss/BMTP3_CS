namespace BMTP3.Core4.Devices;

internal interface IMediaDirectory : IMediaItem
{
	IReadOnlyList<IMediaDirectory> Directories { get; }
	IReadOnlyList<IMediaFile> Files { get; }

	/// <summary>
	/// Lazily enumerates files in this directory, one at a time.
	/// Use instead of <see cref="Files"/> when streaming results with
	/// progress reporting or cancellation between items.
	/// </summary>
	IEnumerable<IMediaFile> EnumerateFiles();

	/// <summary>
	/// Lazily enumerates subdirectories in this directory, one at a time.
	/// Use instead of <see cref="Directories"/> when streaming results with
	/// progress reporting or cancellation between items.
	/// </summary>
	IEnumerable<IMediaDirectory> EnumerateDirectories();
}
