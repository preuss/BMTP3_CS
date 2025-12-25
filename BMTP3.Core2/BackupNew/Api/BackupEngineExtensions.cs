using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace BMTP3.Core2.BackupNew.Api;
public static class BackupEngineExtensions
{
	/// <summary>
	/// Run the engine and expose progress as an IAsyncEnumerable&lt;BackupProgress&gt; without changing IBackupEngine.
	/// Adapter uses a bounded channel (DropOldest) so producer is not blocked by slow consumers.
	/// </summary>
	public static async IAsyncEnumerable<IBackupProgress> RunAsStream(
		this IBackupEngine engine,
		BackupPlan job,
		[EnumeratorCancellation] CancellationToken ct)
	{
		var options = new BoundedChannelOptions(capacity: 4)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.DropOldest
		};
		var channel = Channel.CreateBounded<IBackupProgress>(options);

		// IProgress adapter that producers can use safely from any thread.
		IProgress<IBackupProgress> progress = new Progress<IBackupProgress>(p =>
		{
			// TryWrite is non-blocking; DropOldest prevents unbounded growth.
			channel.Writer.TryWrite(p);
		});

		// Start engine in background and ensure channel is completed when it finishes.
		var runTask = engine.RunAsync(job, progress, ct);
		_ = runTask.ContinueWith(t =>
		{
			// Propagate exception if any; otherwise complete normally.
			if(t.IsFaulted && t.Exception != null)
			{
				channel.Writer.TryComplete(t.Exception);
			} else
			{
				channel.Writer.TryComplete();
			}
		}, TaskScheduler.Default);

		// Yield items as they arrive; consumer can drain between renders.
		await foreach(var item in channel.Reader.ReadAllAsync(ct).WithCancellation(ct))
		{
			yield return item;
		}

		// Observe completion (propagate exceptions if desired).
		await runTask.ConfigureAwait(false);
	}

	public static IBackupJobHandle StartInBackground(
		this IBackupEngine engine,
		BackupPlan job,
		CancellationToken ct = default)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var channel = Channel.CreateBounded<IBackupProgress>(
			new BoundedChannelOptions(4) { FullMode = BoundedChannelFullMode.DropOldest });

		var progress = new Progress<IBackupProgress>(p => channel.Writer.TryWrite(p));

		var task = Task.Run(async () =>
		{
			try
			{
				var result = await engine.RunAsync(job, progress, cts.Token);
				channel.Writer.Complete();
				return result;
			}
			catch (Exception ex)
			{
				channel.Writer.Complete(ex);
				throw;
			}
		}, cts.Token);

		return new BackupJobHandle(task, channel.Reader, cts);
	}
}