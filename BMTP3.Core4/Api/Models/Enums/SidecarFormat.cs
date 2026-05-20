namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the format used for sidecar files accompanying backed-up files.
/// Sidecar files store metadata such as hashes, timestamps, and checksums.
/// </summary>
public enum SidecarFormat
{
	/// <summary>
	/// No sidecar files are generated.
	/// </summary>
	None,

	/// <summary>
	/// Sidecar files are written as INI-style key-value pairs.
	/// </summary>
	Ini,

	/// <summary>
	/// Sidecar files are written as JSON objects.
	/// </summary>
	Json
}
