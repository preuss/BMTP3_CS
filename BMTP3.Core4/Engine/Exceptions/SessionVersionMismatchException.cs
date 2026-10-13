namespace BMTP3.Core4.Engine.Exceptions;

/// <summary>
/// Thrown when a persisted session file was written by an incompatible format version.
/// Behaves exactly like unreadable session data: nothing catches it on the way out —
/// the file cannot be used at all, regardless of resume behavior — it must be discarded
/// and the run started over.
/// </summary>
public sealed class SessionVersionMismatchException : Exception
{
	public int FoundVersion { get; }
	public int ExpectedVersion { get; }
	public FileInfo? SessionFile { get; }

	public SessionVersionMismatchException(int foundVersion, int expectedVersion, FileInfo? sessionFile)
		: base($"Session version mismatch: the session was written by version {foundVersion}, this build expects version {expectedVersion}. " +
			   $"The session format is not backward compatible, so the file cannot be used and must be discarded. " +
			   $"Delete the session file and run again: {sessionFile?.FullName ?? "unknown"}")
	{
		FoundVersion = foundVersion;
		ExpectedVersion = expectedVersion;
		SessionFile = sessionFile;
	}
}
