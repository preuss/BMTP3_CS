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
	private readonly IAnsiConsole _console;
	private readonly bool _debug;

	public BackupProgressDisplay(IAnsiConsole console, bool debug = false)
	{
		_console = console;
		_debug = debug;
	}

	public async Task RunAsync(
		string jobName,
		Func<Action<ProgressReport>, Task> engineRunAsync)
	{
		await _console.Progress()
			.AutoClear(true)
			.AutoRefresh(true)
			.HideCompleted(false)
			.Columns(new ProgressColumn[]
			{
				new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence7)),
				new CounterColumn(),
				new ProgressBarColumn() { Width = 10 },
				new PercentageColumn(),
				new RemainingTimeColumn(),
				new ValueOfMaxColumn(),
				new ElapsedTimeAdvancedColumn(),
			})
			.StartAsync(async ctx =>
			{
				ProgressTask overallTask = ctx.AddTask($"[green]{jobName.EscapeMarkup()}[/]");
				Dictionary<string, FileTaskState> fileTasks = new();

				await engineRunAsync(report =>
				{
					UpdateOverall(overallTask, report);
					UpdateFileTasks(ctx, fileTasks, report);

					if(_debug)
					{
						System.Console.WriteLine(
							$"DEBUG: {report.ActiveFileName} {report.ActiveFileBytesRead}/{report.ActiveFileBytesTotal} Files={report.FilesCompleted}/{report.FilesTotal}");
					}
				});

				RemoveAllFileTasks(ctx, fileTasks);
				overallTask.Value = overallTask.MaxValue;
			});
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

		// Add or update active file sub-task
		if(!string.IsNullOrWhiteSpace(report.ActiveFileName)
		   && report.ActiveFileBytesTotal > 0)
		{
			if(!fileTasks.TryGetValue(report.ActiveFileName, out FileTaskState? state))
			{
				state = new FileTaskState(ctx.AddTask(report.ActiveFileName));
				fileTasks.Add(report.ActiveFileName, state);
			}

			state.LastUpdateUtc = now;
			state.Task.MaxValue = Math.Max(1, report.ActiveFileBytesTotal);
			state.Task.Value = Math.Min(report.ActiveFileBytesRead, state.Task.MaxValue);
			state.Task.Description = report.ActiveFileName;
		}

		// Remove tasks that haven't been updated in 5 seconds
		if(fileTasks.Count == 0)
			return;

		List<string>? expired = null;
		foreach(KeyValuePair<string, FileTaskState> kvp in fileTasks)
		{
			if((now - kvp.Value.LastUpdateUtc).TotalSeconds >= 5)
			{
				(expired ??= new List<string>()).Add(kvp.Key);
			}
		}

		if(expired is not null)
		{
			foreach(string key in expired)
			{
				ctx.RemoveTask(fileTasks[key].Task);
				fileTasks.Remove(key);
			}
		}
	}

	private static void RemoveAllFileTasks(ProgressContext ctx, Dictionary<string, FileTaskState> fileTasks)
	{
		foreach(FileTaskState state in fileTasks.Values)
		{
			ctx.RemoveTask(state.Task);
		}
		fileTasks.Clear();
	}

	private sealed class FileTaskState
	{
		public ProgressTask Task { get; }
		public DateTime LastUpdateUtc { get; set; }

		public FileTaskState(ProgressTask task)
		{
			Task = task;
			LastUpdateUtc = DateTime.UtcNow;
		}
	}
}
