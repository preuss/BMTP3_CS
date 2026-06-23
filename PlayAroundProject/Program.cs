using System.Diagnostics;

namespace PlayAroundProject;

internal class Program
{
	static async Task Main(string[] args)
	{
		string sourcePath = args.Length > 0 ? args[0] : @"C:\temp\source\install.esd";

		if(!File.Exists(sourcePath))
		{
			Console.WriteLine($"File not found: {sourcePath}");
			return;
		}

		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;
		Console.WriteLine($"Source: {sourcePath}");
		Console.WriteLine($"Size:   {fileSize:N0} bytes ({fileSize / (1024.0 * 1024.0):F2} MB)");
		Console.WriteLine();

		string tempDir = Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory;

		// === Baseline: File.Copy ===
		Console.WriteLine("=== File.Copy (reference) ===");
		string temp1 = Path.Combine(tempDir, $"copy_filecopy_{Guid.NewGuid():n}.tmp");
		Stopwatch sw = Stopwatch.StartNew();
		File.Copy(sourcePath, temp1, overwrite: true);
		sw.Stop();
		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;
		Console.WriteLine($"File.Copy:        {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");
		File.Delete(temp1);
		Console.WriteLine();

		// === Manual copy with different buffer sizes ===
		Console.WriteLine("=== Manual Copy (ReadAsync/WriteAsync) ===");
		int[] bufferSizes = { 80 * 1024, 256 * 1024, 512 * 1024, 1024 * 1024 };
		string[] labels = { "80KB", "256KB", "512KB", "1MB" };

		for(int i = 0; i < bufferSizes.Length; i++)
		{
			int buf = bufferSizes[i];
			string label = labels[i];
			string temp2 = Path.Combine(tempDir, $"copy_manual_{buf}_{Guid.NewGuid():n}.tmp");

			sw.Restart();
			await using(FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, buf, FileOptions.Asynchronous | FileOptions.SequentialScan))
			await using(FileStream dest = new(temp2, FileMode.CreateNew, FileAccess.Write, FileShare.None, buf, FileOptions.Asynchronous))
			{
				byte[] buffer = new byte[buf];
				int bytesRead;
				while((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buf))) > 0)
				{
					await dest.WriteAsync(buffer.AsMemory(0, bytesRead));
				}
			}
			sw.Stop();

			mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;
			Console.WriteLine($"Buffer {label,6}: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");
			File.Delete(temp2);
		}

		Console.WriteLine();
		Console.WriteLine("Done.");
	}
}
