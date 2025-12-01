using System.IO;

namespace BMTP3.Core2.BackupNew2.Interfaces;

/// <summary>
/// Abstraction for accessing the content of a source file, regardless of whether it is 
/// on a local file system or a portable device (MTP).
/// </summary>
public interface ISourceContent
{
    /// <summary>
    /// The name of the file (e.g., "image.jpg").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The full path to the file on the source medium.
    /// </summary>
    string OriginalPath { get; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    long SizeBytes { get; }

    /// <summary>
    /// Opens a stream to read the file content.
    /// CALLER IS RESPONSIBLE FOR DISPOSING THE STREAM.
    /// </summary>
    Stream OpenReadStream();
}
