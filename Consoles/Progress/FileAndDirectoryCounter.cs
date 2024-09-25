using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles.Progress {
	internal class FileAndDirectoryCounter {
		private readonly Action<string>? statusCallback;
		private readonly Action<int>? fileIncrementCallback;
		private readonly Action<int>? dirIncrementCallback;
		private readonly CancellationTokenSource cancellationTokenSource;

		private readonly object _lock = new object();
		private int fileCount;
		private int directoryCount;

		public FileAndDirectoryCounter(ProgressTask task) : this((update) => task.Description = update) { }
		public FileAndDirectoryCounter(StatusContext ctx) : this((update) => ctx.Status(update)) { }

		public FileAndDirectoryCounter(Action<string>? statusCallback = default, Action<int>? fileIncrementCallback = default, Action<int>? dirIncrementCallback = default) {
			this.statusCallback = statusCallback;
			this.fileIncrementCallback = fileIncrementCallback;
			this.dirIncrementCallback = dirIncrementCallback;
			cancellationTokenSource = new CancellationTokenSource();

			Task.Run(async () => await UpdateElapsedTime(), cancellationTokenSource.Token);
		}

		public void AddFileIncrement(int count) {
			lock(_lock) {
				fileCount += count;
			}
			fileIncrementCallback?.Invoke(fileCount);
			UpdateStatus();
		}

		public void AddDirectoryIncrement(int count) {
			lock(_lock) {
				directoryCount += count;
			}
			dirIncrementCallback?.Invoke(directoryCount);
			UpdateStatus();
		}

		private void UpdateStatus() {
			statusCallback?.Invoke($"Count files: {fileCount}, Count dirs: {directoryCount}");
		}
		public void StopUpdating() {
			cancellationTokenSource.Cancel();
		}

		private async Task UpdateElapsedTime() {
			while(!cancellationTokenSource.Token.IsCancellationRequested) {
				fileIncrementCallback?.Invoke(fileCount);
				dirIncrementCallback?.Invoke(directoryCount);
				UpdateStatus();
				await Task.Yield();
			}
		}
	}
}
