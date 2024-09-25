using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	internal class ProgressStatusContext {
		public ProgressStatusContext(ProgressContext progressContext) {
			ProgressContext = progressContext;
		}
		public ProgressContext ProgressContext { get; }

		/// <summary>
		/// Gets a value indicating whether or not all started tasks have completed.
		/// </summary>
		public bool IsFinished {
			get {
				return ProgressContext.IsFinished;
			}
		}

		/// <summary>
		/// Adds a task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="autoStart">Whether or not the task should start immediately.</param>
		/// <param name="maxValue">The task's max value.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTask(string description, bool autoStart = true, double maxValue = 100) {
			return new ProgressStatusTask(ProgressContext.AddTask(description, autoStart, maxValue));
		}


		/// <summary>
		/// Adds a task before the reference task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="referenceProgressTask">The reference task to add before.</param>
		/// <param name="autoStart">Whether or not the task should start immediately.</param>
		/// <param name="maxValue">The task's max value.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTaskBefore(string description, ProgressTask referenceProgressTask, bool autoStart = true, double maxValue = 100) {
			return new ProgressStatusTask(ProgressContext.AddTaskBefore(description, referenceProgressTask, autoStart, maxValue));
		}

		/// <summary>
		/// Adds a task after the reference task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="referenceProgressTask">The reference task to add after.</param>
		/// <param name="autoStart">Whether or not the task should start immediately.</param>
		/// <param name="maxValue">The task's max value.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTaskAfter(string description, ProgressTask referenceProgressTask, bool autoStart = true, double maxValue = 100) {
			return new ProgressStatusTask(ProgressContext.AddTaskAfter(description, referenceProgressTask, autoStart, maxValue));
		}

		/// <summary>
		/// Adds a task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="settings">The task settings.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTask(string description, ProgressTaskSettings settings) {
			return new ProgressStatusTask(ProgressContext.AddTask(description, settings));
		}

		/// <summary>
		/// Adds a task at the specified index.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="settings">The task settings.</param>
		/// <param name="index">The index at which the task should be inserted.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTaskAt(string description, ProgressTaskSettings settings, int index) {
			return new ProgressStatusTask(ProgressContext.AddTaskAt(description, settings, index));
		}

		/// <summary>
		/// Adds a task before the reference task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="settings">The task settings.</param>
		/// <param name="referenceProgressTask">The reference task to add before.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTaskBefore(string description, ProgressTaskSettings settings, ProgressTask referenceProgressTask) {
			return new ProgressStatusTask(ProgressContext.AddTaskBefore(description, settings, referenceProgressTask));
		}

		/// <summary>
		/// Adds a task after the reference task.
		/// </summary>
		/// <param name="description">The task description.</param>
		/// <param name="settings">The task settings.</param>
		/// <param name="referenceProgressTask">The reference task to add after.</param>
		/// <returns>The newly created task.</returns>
		public ProgressStatusTask AddTaskAfter(string description, ProgressTaskSettings settings, ProgressTask referenceProgressTask) {
			return new ProgressStatusTask(ProgressContext.AddTaskAfter(description, settings, referenceProgressTask));
		}

		/// <summary>
		/// Refreshes the current progress.
		/// </summary>
		public void Refresh() {
			ProgressContext.Refresh();
		}
	}
}
