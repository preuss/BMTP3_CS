namespace BMTP3.Core4.Traversal;

/// <summary>
/// Parsed components of an <c>mtp://</c> URI.
/// Format: <c>mtp://{DeviceName}/{DriveName}/{DirectoryPath}</c>
/// </summary>
/// <param name="DeviceName">The device's friendly name (e.g. "Apple iPad").</param>
/// <param name="DriveName">The storage/drive name (e.g. "Internal Storage").</param>
/// <param name="DirectoryPath">Subdirectory path within the drive (e.g. "DCIM/Camera"). Empty if root.</param>
public readonly record struct MtpUriParseResult(string DeviceName, string DriveName, string DirectoryPath);