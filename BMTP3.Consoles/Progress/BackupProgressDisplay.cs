using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using BMTP3.Consoles.IO.Consoles.ProgressStatus;
using BMTP3.Consoles.IO.Consoles.Spinner;
using BMTP3.Core4.Api.Models;
using Spectre.Console;

namespace BMTP3.Consoles.Progress;

internal sealed record ProgressReport(
	int FilesCompleted,
	int FilesTotal,
	int DirectoriesTraversed,
	int FilesDiscovered,
	string Phase,
	string? ActiveFileName,
	long ActiveFileBytesRead,
	long ActiveFileBytesTotal
);

public sealed class BackupProgressDisplay
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
		Func<IProgress<BackupProgress>, Task<TResult>> operation)
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

				IProgress<BackupProgress> progress = new BackupProgressRenderer(
					ctx,
					overallTask,
					fileTasks,
					_debug);

				try
				{
					TResult result = await operation(progress);
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

		if (!string.IsNullOrWhiteSpace(report.ActiveFileName)
			&& report.ActiveFileBytesTotal > 0)
		{
			FileTaskState state = GetOrCreateFileTask(ctx, fileTasks, report.ActiveFileName);

			state.LastUpdateUtc = now;
			state.Task.MaxValue = Math.Max(1, report.ActiveFileBytesTotal);
			state.Task.Value = Math.Min(report.ActiveFileBytesRead, state.Task.MaxValue);
			state.Task.Description = report.ActiveFileName.EscapeMarkup();
		}

		RemoveExpiredFileTasks(ctx, fileTasks, now);
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

		FileTaskState created = new(ctx.AddTask(fileName.EscapeMarkup()));
		fileTasks.Add(fileName, created);

		return created;
	}

	private static void RemoveExpiredFileTasks(
		ProgressContext ctx,
		Dictionary<string, FileTaskState> fileTasks,
		DateTime now)
	{
		foreach ((string key, FileTaskState state) in fileTasks
					 .Where(kvp => now - kvp.Value.LastUpdateUtc >= FileTaskExpiration)
					 .ToArray())
		{
			ctx.RemoveTask(state.Task);
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

	private static void WriteDebugLine(bool debug, ProgressReport report)
	{
		if (!debug)
		{
			return;
		}

		Console.WriteLine($"DEBUG: {report.ActiveFileName} {report.ActiveFileBytesRead}/{report.ActiveFileBytesTotal} Files={report.FilesCompleted}/{report.FilesTotal}");
	}

	private sealed class BackupProgressRenderer : IProgress<BackupProgress>
	{
		private readonly ProgressContext _context;
		private readonly ProgressTask _overallTask;
		private readonly Dictionary<string, FileTaskState> _fileTasks;
		private readonly bool _debug;

		public BackupProgressRenderer(
			ProgressContext context,
			ProgressTask overallTask,
			Dictionary<string, FileTaskState> fileTasks,
			bool debug)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_overallTask = overallTask ?? throw new ArgumentNullException(nameof(overallTask));
			_fileTasks = fileTasks ?? throw new ArgumentNullException(nameof(fileTasks));
			_debug = debug;
		}

		public void Report(BackupProgress value)
		{
			ArgumentNullException.ThrowIfNull(value);

			ProgressReport report = ProgressReportMapper.ToReport(value);

			UpdateOverall(_overallTask, report);
			UpdateFileTasks(_context, _fileTasks, report);
			WriteDebugLine(_debug, report);
		}
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