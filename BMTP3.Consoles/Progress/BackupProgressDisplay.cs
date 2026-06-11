using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using BMTP3.Consoles.IO.Consoles.ProgressStatus;
using BMTP3.Consoles.IO.Consoles.Spinner;
using Spectre.Console;

namespace BMTP3.Consoles.Progress;

public sealed record ProgressReport(
	int FilesCompleted,
	int FilesTotal,
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
				object gate = new();

				ProgressReport? latestReport = null;
				int latestReportVersion = 0;
				int processedReportVersion = 0;

				Task engineTask = engineRunAsync(report =>
				{
					lock(gate)
					{
						latestReport = report;
						latestReportVersion++;
					}

					if(_debug)
					{
						Console.WriteLine(
							$"DEBUG: {report.ActiveFileName} {report.ActiveFileBytesRead}/{report.ActiveFileBytesTotal} Files={report.FilesCompleted}/{report.FilesTotal}");
					}
				});

				while(!engineTask.IsCompleted)
				{
					ProgressReport? reportToProcess = null;

					lock(gate)
					{
						if(latestReportVersion != processedReportVersion)
						{
							reportToProcess = latestReport;
							processedReportVersion = latestReportVersion;
						}
					}

					if(reportToProcess != null)
					{
						UpdateOverall(overallTask, reportToProcess);
						UpdateFileTasks(ctx, fileTasks, reportToProcess);
					}

					RemoveExpiredInactiveTasks(ctx, fileTasks);

					await Task.Delay(100);
				}

				await engineTask;
				overallTask.Value = overallTask.MaxValue;
			});
	}
	private static void MarkRemainingCompletedTasksAsInactive(Dictionary<string, FileTaskState> fileTasks)
	{
		DateTime now = DateTime.UtcNow;

		foreach(FileTaskState state in fileTasks.Values)
		{
			bool isComplete = state.Task.Value >= state.Task.MaxValue;
			if(isComplete && state.BecameInactiveAt == null)
			{
				state.BecameInactiveAt = now;
			}
		}
	}


	private static void UpdateOverall(ProgressTask overallTask, ProgressReport report)
	{
		overallTask.MaxValue = Math.Max(1, report.FilesTotal);
		overallTask.Value = Math.Min(report.FilesCompleted, overallTask.MaxValue);
		overallTask.Description = $"{report.Phase}: {report.FilesCompleted}/{report.FilesTotal} files";
	}

	private static void UpdateFileTasks(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks,
		ProgressReport report
	)
	{
		DateTime now = DateTime.UtcNow;

		foreach(FileTaskState state in fileTasks.Values)
		{
			state.SeenInCurrentUpdate = false;
		}

		if(!string.IsNullOrWhiteSpace(report.ActiveFileName) 
		   && report.ActiveFileBytesTotal > 0)
		{
			if(!fileTasks.TryGetValue(report.ActiveFileName, out FileTaskState? activeState))
			{
				ProgressTask task = ctx.AddTask(report.ActiveFileName ?? "");
				activeState = new FileTaskState(task);
				fileTasks.Add(report.ActiveFileName, activeState);
			}

			activeState.SeenInCurrentUpdate = true;
			activeState.BecameInactiveAt = null;

			activeState.Task.Description = report.ActiveFileName ?? "";
			activeState.Task.MaxValue = Math.Max(1, report.ActiveFileBytesTotal);
			activeState.Task.Value = Math.Min(report.ActiveFileBytesRead, activeState.Task.MaxValue);
		}

		foreach(FileTaskState state in fileTasks.Values)
		{
			if(state.SeenInCurrentUpdate)
				continue;

			bool isComplete = state.Task.Value >= state.Task.MaxValue;

			if(isComplete && state.BecameInactiveAt == null)
			{
				state.BecameInactiveAt = now;
			}
		}
	}

	private static void RemoveExpiredInactiveTasks(ProgressContext ctx, Dictionary<string, FileTaskState> fileTasks)
	{
		DateTime now = DateTime.UtcNow;

		List<string> expiredKeys = fileTasks
			.Where(kvp =>
				kvp.Value.BecameInactiveAt is not null &&
				(now - kvp.Value.BecameInactiveAt.Value).TotalSeconds >= 5)
			.Select(kvp => kvp.Key)
			.ToList();

		foreach(string key in expiredKeys)
		{
			ctx.RemoveTask(fileTasks[key].Task);
			fileTasks.Remove(key);
		}
	}

	private sealed class FileTaskState
	{
		public ProgressTask Task { get; }
		public bool SeenInCurrentUpdate { get; set; }
		public DateTime? BecameInactiveAt { get; set; }

		public FileTaskState(ProgressTask task)
		{
			Task = task;
		}
	}
}