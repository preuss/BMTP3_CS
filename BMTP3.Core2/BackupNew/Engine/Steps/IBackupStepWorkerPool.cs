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
	public Task RunAsync(TContext context, IBackupItemStep<TContext, TResult> step, ChannelReader<IBackupItem> reader, ChannelWriter<IBackupItem> writer, int parallelism, CancellationToken ct);
}