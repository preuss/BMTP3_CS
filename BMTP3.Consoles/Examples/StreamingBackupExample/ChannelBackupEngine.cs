using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Response;

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
		DateTimeOffset start = DateTimeOffset.UtcNow;
		for (int i = 0; i < 100; i++)
		{
			ct.ThrowIfCancellationRequested();

			int failed = i % 7 == 0 ? 1 : 0;
			int skipped = i % 13 == 0 ? 1 : 0;
			int processed = i;
			int succeeded = Math.Max(0, processed - failed - skipped);

			BackupProgress snapshot = new()
			{
				State = BackupState.Running,
				Phase = BackupPhase.Initializing,
				StartedAt = start,
				Elapsed = DateTimeOffset.UtcNow - start,
				DirectoriesTraversed = i + 10,
				FilesDiscovered = 100,
				BytesTotal = 100 * 1024L,
				FilesProcessed = processed,
				FilesSucceeded = succeeded,
				FilesSkipped = skipped,
				FilesFailed = failed,
				BytesProcessed = processed * 1024L,
				ActiveFiles = new List<FileProgress>()
			};

			_channel.Writer.TryWrite(snapshot);
			progress?.Report(snapshot);
			await Task.Delay(25, ct).ConfigureAwait(false);
		}

		_channel.Writer.Complete();
		return new BackupJobResult
		{
			JobName = job.Name,
			StartTime = start,
			EndTime = DateTimeOffset.UtcNow,
			State = BackupState.Completed,
			StopReason = StopReason.None,
			FinalProgress = new BackupProgress
			{
				State = BackupState.Completed,
				Phase = BackupPhase.None,
				StartedAt = start,
				Elapsed = DateTimeOffset.UtcNow - start,
				FilesDiscovered = 100,
				FilesProcessed = 100,
				FilesSucceeded = 100,
				ActiveFiles = new List<FileProgress>()
			}
		};
	}

	public IAsyncEnumerable<BackupProgress> StreamAsync(CancellationToken ct)
	{
		return _channel.Reader.ReadAllAsync(ct);
	}
}