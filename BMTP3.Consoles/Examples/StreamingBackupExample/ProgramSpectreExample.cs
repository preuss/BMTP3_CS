using System.Diagnostics;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using Spectre.Console;

namespace BMTP3.Consoles.Examples.StreamingBackupExample;

public static class ProgramSpectreExample
{
	public static async Task RunExampleAsync(ChannelBackupEngine engine, CancellationToken ct)
	{
		IAsyncEnumerable<BackupProgress> stream = engine.StreamAsync(ct);
		TimeSpan renderInterval = TimeSpan.FromMilliseconds(150);
		Stopwatch stopwatch = Stopwatch.StartNew();
		BackupProgress? last = null;

		await foreach (BackupProgress p in stream.WithCancellation(ct))
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
		Table table = new();
		table.AddColumn("Metric");
		table.AddColumn("Value");
		table.AddRow("Processed", $"{p.FilesProcessed}/{p.FilesDiscovered}");
		table.AddRow("Percent", $"{p.PercentageComplete:0.0}%");
		foreach (FileProgress file in p.ActiveFiles)
		{
			table.AddRow("Active File", $"{file.FileName} ({file.BytesProcessed}/{file.BytesTotal} bytes)");
			table.AddRow("File Phase", file.Phase.ToString());
			table.AddRow("Source Path", file.SourcePath);
			table.AddRow("Relative Path", file.RelativePath);
		}

		AnsiConsole.Write(table);
	}
}