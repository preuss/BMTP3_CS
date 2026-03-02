using BMTP3.Core2.BackupNew.Api.Progress;
using Spectre.Console;
using System.Diagnostics;

namespace BMTP3.Consoles.Examples.StreamingBackupExample;

public static class ProgramSpectreExample
{
	public static async Task RunExampleAsync(ChannelBackupEngine engine, CancellationToken ct)
	{
		var stream = engine.StreamAsync(ct);
		var renderInterval = TimeSpan.FromMilliseconds(150);
		var stopwatch = Stopwatch.StartNew();
		BackupProgress? last = null;

		await foreach(var p in stream.WithCancellation(ct))
		{
			// Keep the latest snapshot; channel is configured to DropOldest so producer won't block
			last = p;

			if(stopwatch.Elapsed >= renderInterval)
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
		table.AddRow("Processed", $"{p.FilesProcessed}/{p.FilesDiscovered}");
		table.AddRow("Percent", $"{p.PercentageComplete:0.0}%");
		foreach(var file in p.ActiveFiles)
		{
			table.AddRow("Active File", $"{file.FileName} ({file.BytesProcessed}/{file.BytesTotal} bytes)");
			table.AddRow("File Phase", file.Phase.ToString());
			table.AddRow("Source Path", file.SourcePath);
			table.AddRow("Relative Path", file.RelativePath);
		}
		AnsiConsole.Write(table);
	}
}
