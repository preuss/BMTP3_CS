using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Job;

namespace BMTP3.Consoles.Examples.StreamingBackupExample;

public class ChannelBackupEngine : IBackupEngine
{
	private readonly Channel<BackupProgress> _channel;

	public ChannelBackupEngine()
	{
		BoundedChannelOptions options = new(4)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.DropOldest
		};
		_channel = Channel.CreateBounded<BackupProgress>(options);
	}

	public ChannelWriter<BackupProgress> Writer => _channel.Writer;

	public async Task<BackupJobResult> RunAsync(BackupPlan job, IProgress<IBackupProgress> progress,
		CancellationToken ct)
	{
		// Example implementation that writes into the channel while still supporting IProgress for backwards compatibility
		for (int i = 0; i < 100; i++)
		{
			ct.ThrowIfCancellationRequested();

			int failed = i % 7 == 0 ? 1 : 0;
			int skipped = i % 13 == 0 ? 1 : 0;
			int processed = i;
			int succeeded = Math.Max(0, processed - failed - skipped);

			BackupProgress snapshot = new()
			{
				Phase = BackupPhase.Starting,
				DirectoriesTraversed = i + 10,
				FilesDiscovered = 100,
				BytesTotal = 100 * 1024L,

				FilesProcessed = processed,
				FilesSucceeded = succeeded,
				FilesSkipped = skipped,
				FilesFailed = failed,

				BytesProcessed = processed * 1024L,

				// Required member on BackupProgress: make a reasonable empty/default active-files list for the example
				ActiveFiles = new List<FileProgress>()
			};

			// Try write to channel without awaiting to avoid blocking producer
			_channel.Writer.TryWrite(snapshot);

			// Keep backwards compatibility by reporting via IProgress
			progress?.Report(snapshot);

			await Task.Delay(25, ct).ConfigureAwait(false);
		}

		_channel.Writer.Complete();
		return new BackupJobResult { Status = JobState.Completed };
	}

	public IAsyncEnumerable<BackupProgress> StreamAsync(CancellationToken ct)
	{
		return _channel.Reader.ReadAllAsync(ct);
	}
}