namespace BMTP3.Core4.Scanner;

internal sealed record FileEntry(string RelativePath, long Length, DateTimeOffset? ModifiedAt);

internal interface IScanSource : IDisposable, IAsyncDisposable
{
	string RootPath { get; }

	Task OpenAsync(CancellationToken cancellationToken);

	IAsyncEnumerable<string> EnumerateDirectoriesAsync(string directoryRelativePath, CancellationToken cancellationToken);

	IAsyncEnumerable<FileEntry> EnumerateFilesAsync(string directoryRelativePath, CancellationToken cancellationToken);

	Stream OpenRead(string relativePath);
}
