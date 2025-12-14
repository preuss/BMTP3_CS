using BMTP3.Core2.BackupNew.Domain.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;
public interface IBackupStepWorkerPool<TContext, TResult>
{
	int Parallelism { get; }
	TContext Context { get; }
	IBackupItemStep<TContext, TResult> ItemStep { get; }
	public Task RunAsync(ChannelReader<IBackupItem> reader, ChannelWriter<IBackupItem> writer, CancellationToken ct);
}