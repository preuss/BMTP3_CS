using System.Threading.Channels;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Response;

namespace BMTP3.Core2.BackupNew.Api;

internal sealed class BackupJobHandle : IBackupJobHandle
{
	private readonly CancellationTokenSource _cts;
	private readonly ChannelReader<IBackupProgress> _progressReader;

	public BackupJobHandle(Task<BackupJobResult> completion, ChannelReader<IBackupProgress> progressReader,
		CancellationTokenSource cts)
	{
		Completion = completion ?? throw new ArgumentNullException(nameof(completion));
		_progressReader = progressReader ?? throw new ArgumentNullException(nameof(progressReader));
		_cts = cts ?? throw new ArgumentNullException(nameof(cts));
	}

	public Task<BackupJobResult> Completion { get; }

	public bool IsCompleted => Completion.IsCompleted;

	public void Cancel()
	{
		_cts.Cancel();
	}

	public IAsyncEnumerable<IBackupProgress> ObserveProgress(CancellationToken ct = default)
	{
		return _progressReader.ReadAllAsync(ct);
	}
}