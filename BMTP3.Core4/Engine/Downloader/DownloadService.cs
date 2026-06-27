using BMTP3.Core4.Models;
using System.Runtime.InteropServices;

namespace BMTP3.Core4.Engine.Downloader;
internal sealed class DownloadService : IDownloadService
{
	private const int BufferSize = 4 * 1024 * 1024; // 4 MB buffer for efficient stream copying

	public async Task DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken)
	{
		Stream sourceStream;
		try
		{
			sourceStream = await request.Item.Content.OpenReadAsync(cancellationToken);
		}
		catch (COMException ex)
		{
			throw new IOException(
				$"Failed to open source file for download. " +
				$"File: '{request.Item.RelativeFilePath}', " +
				$"SourcePath: '{request.Item.SourcePath}', " +
				$"FileName: '{request.Item.FileName}', " +
				$"Length: {request.Item.Content.Length}. " +
				$"COM error: {ex.Message}", ex);
		}

		await using(sourceStream)
		{
			using(FileStream destStream = request.Destination.Create())
			{
				ulong totalBytesRead = 0;
				byte[] buffer = new byte[BufferSize];
				int read;
				while((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
				{
					await destStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
					totalBytesRead += (ulong)read;
					progress?.Report(totalBytesRead);
				}
			}
		}

		//This needs to be after Close() or Dispose() of destStream, else the LastWriteTime and LastAccessTime will be set to the time of the copy, not the original file.
		DateTime backupDateTime = request.BackupStartTime.LocalDateTime;
		request.Destination.CreationTime = request.Item.DateCreated?.LocalDateTime ?? backupDateTime;
		request.Destination.LastWriteTime = request.Item.DateModified?.LocalDateTime ?? backupDateTime;
		request.Destination.LastAccessTime = request.Item.DateAccessed?.LocalDateTime ?? backupDateTime;

		request.Item.ReplaceContentProvider(new MoveableFileContent(request.Destination.FullName));
	}
}