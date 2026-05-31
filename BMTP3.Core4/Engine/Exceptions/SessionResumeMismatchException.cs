namespace BMTP3.Core4.Engine.Exceptions;

public sealed class SessionResumeMismatchException : Exception
{
	public int AddedFiles { get; }
	public int RemovedFiles { get; }

	public SessionResumeMismatchException(int addedFiles, int removedFiles)
		: base($"Resume failed: current source file list does not match persisted session state (added: {addedFiles}, removed: {removedFiles}).")
	{
		AddedFiles = addedFiles;
		RemovedFiles = removedFiles;
	}
}