
using System.Diagnostics;
using System.Threading.Channels;

namespace PlayAroundProject;

internal class Program
{
	static async Task Main(string[] args)
	{
		const int delay = 0;
		string path = @"C:\temp\source\install.esd";
		DirectoryInfo targetDirectoryInfo = new DirectoryInfo(@"D:\temp");

		int[] sizes =
		{
			/*
			64 * 1024,
			80 * 1024,
			128 * 1024,
			256 * 1024,
			512 * 1024,*/
			1 * 1024 * 1024,
			2 * 1024 * 1024,
			4 * 1024 * 1024,
			8 * 1024 * 1024,
			16 * 1024 * 1024,
			32 * 1024 * 1024,
			64 * 1024 * 1024,
			128 * 1024 * 1024,
		};


		Console.WriteLine();
		Console.WriteLine("=== Parallel Disk Benchmark ===");

		int[] parallelChunkSizes =
		{
//			512 * 1024,
			1 * 1024 * 1024,
			2 * 1024 * 1024,
			4 * 1024 * 1024,
			8 * 1024 * 1024,
			16 * 1024 * 1024,
		};

		int[] workerCounts =
		{
			1,
			2,
			4,/*
			8,
			16
			*/
		};

		Random.Shared.Shuffle(parallelChunkSizes);
		foreach(int chunkSize in parallelChunkSizes)
		{
			foreach(int workers in workerCounts)
			{
				//await RunParallelDiskBenchmark(path, targetDirectoryInfo, chunkSize, workers, writeThrough: true);
				//Thread.Sleep(3000);
			}

			Console.WriteLine();
		}
		//Thread.Sleep(10000);


		Console.WriteLine("=== File.Copy (reference) ===");
		RunFileCopyBenchmark(path, targetDirectoryInfo);
		Console.WriteLine();

		Console.WriteLine("=== Manual Disk (WriteThrough) ===");
		Random.Shared.Shuffle(sizes);
		foreach(int size in sizes)
		{
			RunDiskBenchmark(path, targetDirectoryInfo, size);
			//Thread.Sleep(3000);
		}
		//Thread.Sleep(10000);
		Console.WriteLine();

		Console.WriteLine("=== Simulated Device ===");
		Random.Shared.Shuffle(sizes);
		foreach(int size in sizes)
		{
			await RunSimulatedDeviceTest(path, targetDirectoryInfo, size, delay);
			//Thread.Sleep(3000);
		}
		//Thread.Sleep(10000);
		Console.WriteLine();

		Console.WriteLine("=== Pipeline ===");
		Random.Shared.Shuffle(sizes);
		foreach(int size in sizes)
		{
			await RunPipelineTest(path, targetDirectoryInfo, size, delay);
			//Thread.Sleep(3000);
		}
	}

	// 🔥 1. File.Copy baseline (ingen buffer!)
	static void RunFileCopyBenchmark(string sourcePath, DirectoryInfo targetDirectoryInfo)
	{
		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;

		string tempFile = Path.Combine(targetDirectoryInfo.FullName, "filecopy.tmp");

		Stopwatch sw = Stopwatch.StartNew();
		File.Copy(sourcePath, tempFile, overwrite: true);
		sw.Stop();

		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;

		Console.WriteLine($"File.Copy: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");

		SafeDelete(tempFile);
	}

	// 🔹 2. Manual disk (WriteThrough)
	static void RunDiskBenchmark(string sourcePath, DirectoryInfo targetDirectoryInfo, int bufferSize)
	{
		string label = FormatSize(bufferSize);

		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;

		string tempFile = Path.Combine(targetDirectoryInfo.FullName, "disk_test.tmp");

		Stopwatch sw = Stopwatch.StartNew();

		using(FileStream source = new(
			sourcePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize,
			FileOptions.SequentialScan))
		using(FileStream dest = new(
			tempFile,
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			bufferSize,
			FileOptions.WriteThrough))
		{
			dest.SetLength(fileSize);

			byte[] buffer = new byte[bufferSize];
			int read;

			while((read = source.Read(buffer, 0, buffer.Length)) > 0)
			{
				dest.Write(buffer, 0, read);
			}
		}

		sw.Stop();

		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;

		Console.WriteLine($"Disk     [{label,6}]: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");

		SafeDelete(tempFile);
	}

	// 📱 3. Simulated device (ingen WriteThrough = realistisk app)
	static async Task RunSimulatedDeviceTest(string sourcePath, DirectoryInfo targetDirectoryInfo, int chunkSize, int delay = 0)
	{
		string label = FormatSize(chunkSize);

		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;

		string tempFile = Path.Combine(targetDirectoryInfo.FullName, "mtp_test.tmp");

		Stopwatch sw = Stopwatch.StartNew();

		using(FileStream source = new(
				  sourcePath,
				  FileMode.Open,
				  FileAccess.Read,
				  FileShare.Read,
				  chunkSize,
				  FileOptions.Asynchronous | FileOptions.SequentialScan))
		using(FileStream dest = new(
				  tempFile,
				  FileMode.Create,
				  FileAccess.Write,
				  FileShare.None,
				  chunkSize,
				  FileOptions.Asynchronous))
		{
			dest.SetLength(fileSize);

			byte[] buffer = new byte[chunkSize];
			int read;

			while((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				// Slå denne til hvis du vil simulere MTP/device latency:
				if(delay > 0)
					await Task.Delay(delay);

				await dest.WriteAsync(buffer, 0, read);
			}
		}

		sw.Stop();

		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;
		Console.WriteLine($"SimDev   [{label,6}]: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");

		SafeDelete(tempFile);
	}

	// 🚀 4. Pipeline (real-world optimal)
	static async Task RunPipelineTest(string sourcePath, DirectoryInfo targetDirectoryInfo, int chunkSize, int delay = 0)
	{
		string label = FormatSize(chunkSize);

		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;

		string tempFile = Path.Combine(targetDirectoryInfo.FullName, "pipeline_test.tmp");

		Channel<byte[]> channel = Channel.CreateBounded<byte[]>(4);

		Stopwatch sw = Stopwatch.StartNew();

		using(FileStream source = new(
			sourcePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			chunkSize,
			FileOptions.Asynchronous | FileOptions.SequentialScan))
		using(FileStream dest = new(
			tempFile,
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			chunkSize,
			FileOptions.Asynchronous))
		{
			dest.SetLength(fileSize);

			Task producer = Task.Run(async () =>
			{
				try
				{
					while(true)
					{
						byte[] buffer = new byte[chunkSize];
						int read = await source.ReadAsync(buffer, 0, buffer.Length);

						if(read == 0)
							break;

						// Slå denne til for at simulere MTP/USB latency:
						if(delay > 0)
							await Task.Delay(delay);

						if(read != buffer.Length)
						{
							byte[] trimmed = new byte[read];
							Array.Copy(buffer, trimmed, read);
							buffer = trimmed;
						}

						await channel.Writer.WriteAsync(buffer);
					}
				} finally
				{
					channel.Writer.Complete();
				}
			});

			Task consumer = Task.Run(async () =>
			{
				await foreach(byte[] chunk in channel.Reader.ReadAllAsync())
				{
					await dest.WriteAsync(chunk, 0, chunk.Length);
				}
			});

			await Task.WhenAll(producer, consumer);
		}

		sw.Stop();

		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;
		Console.WriteLine($"Pipeline [{label,6}]: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s");

		SafeDelete(tempFile);
	}

	static async Task RunParallelDiskBenchmark(string sourcePath, DirectoryInfo targetDirectoryInfo, int chunkSize, int workerCount, bool writeThrough)
	{
		string chunkLabel = FormatSize(chunkSize);

		FileInfo fi = new(sourcePath);
		long fileSize = fi.Length;

		string tempFile = Path.Combine(targetDirectoryInfo.FullName, $"parallel_test_{workerCount}_{chunkLabel}.tmp");

		FileOptions destOptions = FileOptions.Asynchronous | FileOptions.RandomAccess;

		if(writeThrough)
			destOptions |= FileOptions.WriteThrough;

		Stopwatch sw = Stopwatch.StartNew();

		using(FileStream source = new(
			sourcePath,
			new FileStreamOptions
			{
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read,
				BufferSize = 1,
				Options = FileOptions.Asynchronous | FileOptions.RandomAccess
			}))
		using(FileStream dest = new(
			tempFile,
			new FileStreamOptions
			{
				Mode = FileMode.Create,
				Access = FileAccess.Write,
				Share = FileShare.None,
				BufferSize = 1,
				Options = destOptions
			}))
		{
			// Pre-allocate destination file
			dest.SetLength(fileSize);

			long nextOffset = 0;

			Task[] workers = new Task[workerCount];

			for(int workerIndex = 0; workerIndex < workerCount; workerIndex++)
			{
				workers[workerIndex] = Task.Run(async () =>
				{
					byte[] buffer = new byte[chunkSize];

					while(true)
					{
						long offset = Interlocked.Add(ref nextOffset, chunkSize) - chunkSize;

						if(offset >= fileSize)
							break;

						int bytesToRead = (int)Math.Min(chunkSize, fileSize - offset);

						int totalRead = 0;

						while(totalRead < bytesToRead)
						{
							int read = await RandomAccess.ReadAsync(
								source.SafeFileHandle,
								buffer.AsMemory(totalRead, bytesToRead - totalRead),
								offset + totalRead);

							if(read == 0)
								throw new EndOfStreamException();

							totalRead += read;
						}

						await RandomAccess.WriteAsync(
							dest.SafeFileHandle,
							buffer.AsMemory(0, totalRead),
							offset);
					}
				});
			}

			await Task.WhenAll(workers);

			// Gør målingen mere ærlig, især hvis WriteThrough=false
			dest.Flush(flushToDisk: true);
		}

		sw.Stop();

		double mbps = fileSize / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds;

		Console.WriteLine($"Parallel [{chunkLabel,6}] QD/Workers {workerCount,2}: {sw.Elapsed.TotalSeconds,7:F3}s  {mbps,7:F0} MB/s  WT={writeThrough}");

		SafeDelete(tempFile);
	}

	static void SafeDelete(string path)
	{
		for(int i = 0; i < 5; i++)
		{
			try
			{
				File.Delete(path);
				return;
			} catch(IOException)
			{
				Thread.Sleep(50);
			}
		}
	}

	static string FormatSize(int bytes)
	{
		if(bytes >= 1024 * 1024)
			return $"{bytes / (1024 * 1024)}MB";

		if(bytes >= 1024)
			return $"{bytes / 1024}KB";

		return $"{bytes}B";
	}
}
