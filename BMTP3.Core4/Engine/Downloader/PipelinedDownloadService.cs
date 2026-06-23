using BMTP3.Core4.Models;
using System.Buffers;
using System.Threading.Channels;

namespace BMTP3.Core4.Engine.Downloader;

internal sealed class PipelinedDownloadService : IDownloadService
{
	private const int BufferSize = 1 * 1024 * 1024; // 1 MB buffer for efficient sequential stream copying.
	private const int QueueCapacity = 2;            // ~2 MB read-ahead; still only one reader (1 reader and 1 writer), suitable for MTP/PTP streams.

	public async Task DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken)
	{
		await using Stream sourceStream = await request.Item.Content.OpenReadAsync(cancellationToken);

		long preallocationSize = 0;

		if(sourceStream.CanSeek)
		{
			try
			{
				preallocationSize = (long)request.Item.Content.Length;
			} catch
			{
				preallocationSize = 0;
			}
		}

		FileStreamOptions destinationOptions = new()
		{
			Mode = FileMode.Create,
			Access = FileAccess.Write,
			Share = FileShare.None,
			BufferSize = BufferSize,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
			PreallocationSize = preallocationSize > 0 ? preallocationSize : 0
		};

		await using(FileStream destStream = new(request.Destination.FullName, destinationOptions))
		{
			await CopyWithPipelineAsync(
				sourceStream,
				destStream,
				progress,
				cancellationToken);
		}

		// This needs to be after Close() or Dispose() of destStream,
		// else LastWriteTime and LastAccessTime will be set to the time of the copy,
		// not the original file.
		DateTime backupDateTime = request.BackupStartTime.LocalDateTime;

		request.Destination.CreationTime = request.Item.DateCreated?.LocalDateTime ?? backupDateTime;

		request.Destination.LastWriteTime = request.Item.DateModified?.LocalDateTime ?? backupDateTime;

		request.Destination.LastAccessTime = request.Item.DateAccessed?.LocalDateTime ?? backupDateTime;

		request.Item.ReplaceContentProvider(new MoveableFileContent(request.Destination.FullName));
	}

	private static async Task CopyWithPipelineAsync(
		Stream sourceStream,
		Stream destStream,
		IProgress<ulong>? progress,
		CancellationToken cancellationToken)
	{
		Channel<BufferChunk> channel = Channel.CreateBounded<BufferChunk>(
			new BoundedChannelOptions(QueueCapacity)
			{
				SingleReader = true,
				SingleWriter = true,
				FullMode = BoundedChannelFullMode.Wait
			});

		ulong totalBytesRead = 0;

		Task producer = Task.Run(async () =>
		{
			try
			{
				while(true)
				{
					byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

					int read;
					try
					{
						read = await sourceStream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken);
					} catch
					{
						ArrayPool<byte>.Shared.Return(buffer);
						throw;
					}

					if(read == 0)
					{
						ArrayPool<byte>.Shared.Return(buffer);
						break;
					}

					await channel.Writer.WriteAsync(new BufferChunk(buffer, read), cancellationToken);
				}

				channel.Writer.Complete();
			} catch(Exception ex)
			{
				channel.Writer.Complete(ex);
			}
		}, cancellationToken);

		Task consumer = Task.Run(async () =>
		{
			await foreach(BufferChunk chunk in channel.Reader.ReadAllAsync(cancellationToken))
			{
				try
				{
					await destStream.WriteAsync(chunk.Buffer.AsMemory(0, chunk.Count), cancellationToken);

					totalBytesRead += (ulong)chunk.Count;
					progress?.Report(totalBytesRead);
				} finally
				{
					ArrayPool<byte>.Shared.Return(chunk.Buffer);
				}
			}
		}, cancellationToken);

		await Task.WhenAll(producer, consumer);
	}

	private readonly record struct BufferChunk(byte[] Buffer, int Count);
}