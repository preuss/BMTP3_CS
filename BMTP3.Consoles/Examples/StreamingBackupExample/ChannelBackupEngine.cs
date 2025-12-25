using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Consoles.Examples.StreamingBackupExample;

public class ChannelBackupEngine : IBackupEngine
{
	private readonly Channel<BackupProgress> _channel;

	public ChannelBackupEngine()
	{
		var options = new BoundedChannelOptions(4)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.DropOldest
		};
		_channel = Channel.CreateBounded<BackupProgress>(options);
	}

	public ChannelWriter<BackupProgress> Writer => _channel.Writer;

	public async Task<BackupJobResult> RunAsync(BackupPlan job, IProgress<IBackupProgress> progress, CancellationToken ct)
	{
		// Example implementation that writes into the channel while still supporting IProgress for backwards compatibility
		for (int i = 0; i < 100; i++)
		{
			ct.ThrowIfCancellationRequested();
			var snapshot = new BackupProgress
			{
				TotalFiles = 100,
				ProcessedFiles = i,
				CurrentFileName = $"file_{i}.jpg",
				CurrentActivity = "Processing",
				TotalItemsDiscovered = i + 10,
				ItemsFailed = i % 7 == 0 ? 1 : 0,
				ItemsProcessed = i,
				BytesProcessed = i * 1024,
				ItemsSkipped = i % 13 == 0 ? 1 : 0
			};

			// Try write to channel without awaiting to avoid blocking producer
			_channel.Writer.TryWrite(snapshot);

			// Keep backwards compatibility by reporting via IProgress
			progress?.Report(snapshot);

			await Task.Delay(25, ct).ConfigureAwait(false);
		}

		_channel.Writer.Complete();
		return new BackupJobResult { Success = true };
	}

	public IAsyncEnumerable<BackupProgress> StreamAsync(CancellationToken ct)
	{
		return _channel.Reader.ReadAllAsync(ct);
	}
}
