namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the level of verification performed after a file is written to the destination.
/// </summary>
public enum PostWriteVerificationType
{
	/// <summary>
	/// No verification. Trust the file system.
	/// </summary>
	None,

	/// <summary>
	/// Confirm the destination file is intact by computing and comparing its content hash.
	/// </summary>
	Hash,
}