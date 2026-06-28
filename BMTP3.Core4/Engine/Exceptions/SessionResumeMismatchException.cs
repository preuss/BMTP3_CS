namespace BMTP3.Core4.Engine.Exceptions;

public sealed class SessionResumeMismatchException : Exception
{
	public int AddedFiles { get; }
	public int RemovedFiles { get; }
	public IReadOnlyList<string> NewFiles { get; }
	public IReadOnlyList<string> RemovedFilesPaths { get; }
	public FileInfo? SessionFile { get; }

	private static string FormatList(IReadOnlyList<string> paths, int count)
	{
		if(count == 0) return "";
		int show = Math.Min(5, paths.Count);
		string items = string.Join(", ", paths.Take(show).Select(p => $"'{p}'"));
		int remaining = count - show;
		return remaining > 0 ? $"{items} ... and {remaining} more" : items;
	}

	public SessionResumeMismatchException(
		int addedFiles, int removedFiles,
		IReadOnlyList<string> newFiles, IReadOnlyList<string> removedFilesPaths,
		FileInfo? sessionFile)
		: base($"Resume mismatch: the source file list has changed since the last session " +
			   $"({addedFiles} new, {removedFiles} removed). " +
			   $"New{(addedFiles == 1 ? "" : "s")}: {FormatList(newFiles, addedFiles)}. " +
			   $"Removed: {FormatList(removedFilesPaths, removedFiles)}. " +
			   $"This can happen if the device was reconnected and WPD assigned new file IDs. " +
			   $"Delete the session file and run again: {sessionFile?.FullName ?? "unknown"}")
	{
		AddedFiles = addedFiles;
		RemovedFiles = removedFiles;
		NewFiles = newFiles;
		RemovedFilesPaths = removedFilesPaths;
		SessionFile = sessionFile;
	}
}