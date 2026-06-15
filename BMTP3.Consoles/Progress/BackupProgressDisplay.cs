using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using BMTP3.Consoles.IO.Consoles.ProgressStatus;
using BMTP3.Consoles.IO.Consoles.Spinner;
using Spectre.Console;

namespace BMTP3.Consoles.Progress;

public sealed record ProgressReport(
	int FilesCompleted,
	int FilesTotal,
	int DirectoriesTraversed,
	int FilesDiscovered,
	string Phase,
	string? ActiveFileName,
	long ActiveFileBytesRead,
	long ActiveFileBytesTotal
);

public class BackupProgressDisplay
{
	private static readonly TimeSpan FileTaskExpiration = TimeSpan.FromSeconds(5);

	private readonly IAnsiConsole _console;
	private readonly bool _debug;

	public BackupProgressDisplay(IAnsiConsole console, bool debug = false)
	{
		_console = console ?? throw new ArgumentNullException(nameof(console));
		_debug = debug;
	}

	public async Task<TResult> RunAsync<TResult>(
		string jobName,
		Func<IProgress<ProgressReport>, Task<TResult>> operation)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
		ArgumentNullException.ThrowIfNull(operation);

		return await _console.Progress()
			.AutoClear(true)
			.AutoRefresh(true)
			.HideCompleted(false)
			.Columns(CreateColumns())
			.StartAsync(async ctx =>
			{
				ProgressTask overallTask = ctx.AddTask($"[green]{jobName.EscapeMarkup()}[/]");
				Dictionary<string, FileTaskState> fileTasks = new();

				IProgress<ProgressReport> displayProgress = new Progress<ProgressReport>(report =>
				{
					UpdateOverall(overallTask, report);
					UpdateFileTasks(ctx, fileTasks, report);
					WriteDebugLine(report);
				});

				try
				{
					TResult result = await operation(displayProgress);

					// Only show completed when the operation actually completed.
					overallTask.Value = overallTask.MaxValue;

					return result;
				}
				finally
				{
					RemoveAllFileTasks(ctx, fileTasks);
				}
			});
	}

	private static ProgressColumn[] CreateColumns()
	{
		return new ProgressColumn[]
		{
			new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence7)),
			new CounterColumn(),
			new ProgressBarColumn { Width = 10 },
			new PercentageColumn(),
			new RemainingTimeColumn(),
			new ValueOfMaxColumn(),
			new ElapsedTimeAdvancedColumn(),
		};
	}

	private static void UpdateOverall(ProgressTask overallTask, ProgressReport report)
	{
		overallTask.MaxValue = Math.Max(1, report.FilesTotal);
		overallTask.Value = Math.Min(report.FilesCompleted, overallTask.MaxValue);
		overallTask.Description = report.Phase;
	}

	private static void UpdateFileTasks(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks,
		ProgressReport report)
	{
		DateTime now = DateTime.UtcNow;

		if (HasActiveFile(report))
		{
			FileTaskState state = GetOrCreateFileTask(ctx, fileTasks, report.ActiveFileName!);

			state.LastUpdateUtc = now;
			state.Task.MaxValue = Math.Max(1, report.ActiveFileBytesTotal);
			state.Task.Value = Math.Min(report.ActiveFileBytesRead, state.Task.MaxValue);
			state.Task.Description = report.ActiveFileName!;
		}

		RemoveExpiredFileTasks(ctx, fileTasks, now);
	}

	private static bool HasActiveFile(ProgressReport report)
	{
		return !string.IsNullOrWhiteSpace(report.ActiveFileName)
			&& report.ActiveFileBytesTotal > 0;
	}

	private static FileTaskState GetOrCreateFileTask(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks,
		string fileName)
	{
		if (fileTasks.TryGetValue(fileName, out FileTaskState? state))
		{
			return state;
		}

		FileTaskState created = new(ctx.AddTask(fileName));
		fileTasks.Add(fileName, created);

		return created;
	}

	private static void RemoveExpiredFileTasks(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks,
		DateTime now)
	{
		foreach (string key in fileTasks
			.Where(kvp => now - kvp.Value.LastUpdateUtc >= FileTaskExpiration)
			.Select(kvp => kvp.Key)
			.ToArray())
		{
			ctx.RemoveTask(fileTasks[key].Task);
			fileTasks.Remove(key);
		}
	}

	private static void RemoveAllFileTasks(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks)
	{
		foreach (FileTaskState state in fileTasks.Values)
		{
			ctx.RemoveTask(state.Task);
		}

		fileTasks.Clear();
	}

	private void WriteDebugLine(ProgressReport report)
	{
		if (!_debug)
		{
			return;
		}

		System.Console.WriteLine(
			$"DEBUG: {report.ActiveFileName} {report.ActiveFileBytesRead}/{report.ActiveFileBytesTotal} Files={report.FilesCompleted}/{report.FilesTotal}");
	}

	private sealed class FileTaskState
	{
		public FileTaskState(ProgressTask task)
		{
			Task = task ?? throw new ArgumentNullException(nameof(task));
			LastUpdateUtc = DateTime.UtcNow;
		}

		public ProgressTask Task { get; }

		public DateTime LastUpdateUtc { get; set; }
	}
}