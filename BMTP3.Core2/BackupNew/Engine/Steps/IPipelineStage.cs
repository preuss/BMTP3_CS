using BMTP3.Core2.BackupNew.Domain.Item;
using System.Threading.Channels;

namespace BMTP3.Core2.BackupNew.Engine.Steps;

/// <summary>
/// Represents a parallel processing stage in the backup pipeline.
/// A stage consumes items from a reader, processes them, and writes them to a writer.
/// </summary>
/// <typeparam name="TContext">The context for this stage.</typeparam>
public interface IPipelineStage<TContext>
{
	int Parallelism { get; }
	TContext Context { get; }

	/// <summary>
	/// Starts the worker pool for this stage.
	/// </summary>
	Task RunAsync(ChannelReader<IBackupItem> reader, ChannelWriter<IBackupItem> writer, CancellationToken ct);
}