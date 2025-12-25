using System.Diagnostics;
using BMTP3.Core2.BackupNew.Api.Progress;
using Spectre.Console;

namespace BMTP3.Consoles.Examples.StreamingBackupExample;

public static class ProgramSpectreExample
{
	public static async Task RunExampleAsync(ChannelBackupEngine engine, CancellationToken ct)
	{
		var stream = engine.StreamAsync(ct);
		var renderInterval = TimeSpan.FromMilliseconds(150);
		var stopwatch = Stopwatch.StartNew();
		BackupProgress? last = null;

		await foreach (var p in stream.WithCancellation(ct))
		{
			// Keep the latest snapshot; channel is configured to DropOldest so producer won't block
			last = p;

			if (stopwatch.Elapsed >= renderInterval)
			{
				RenderSnapshot(last!);
				stopwatch.Restart();
			}
		}
	}

	private static void RenderSnapshot(BackupProgress p)
	{
		AnsiConsole.Clear();
		var table = new Table();
		table.AddColumn("Metric");
		table.AddColumn("Value");
		table.AddRow("Processed", $"{p.ProcessedFiles}/{p.TotalFiles}");
		table.AddRow("Percent", $"{p.PercentageComplete:0.0}%");
		table.AddRow("Activity", p.CurrentActivity);
		table.AddRow("File", p.CurrentFileName);
		AnsiConsole.Write(table);
	}
}
