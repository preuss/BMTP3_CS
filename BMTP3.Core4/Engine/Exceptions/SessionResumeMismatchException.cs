namespace BMTP3.Core4.Engine.Exceptions;

public sealed class SessionResumeMismatchException : Exception
{
	public int AddedFiles { get; }
	public int RemovedFiles { get; }
	public FileInfo? SessionFile { get; }

	public SessionResumeMismatchException(int addedFiles, int removedFiles, FileInfo? sessionFile)
		: base($"Resume mismatch: the source file list has changed since the last session " +
			   $"({addedFiles} new, {removedFiles} removed). " +
			   $"This can happen if the device was reconnected and WPD assigned new file IDs. " +
			   $"Delete the session file and run again: {sessionFile?.FullName ?? "unknown"}")
	{
		AddedFiles = addedFiles;
		RemovedFiles = removedFiles;
		SessionFile = sessionFile;
	}
}