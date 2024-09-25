using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	internal class ProgressStatusTask {
		public ProgressStatusTask(ProgressTask progressTask) {
			ProgressTask = progressTask;
		}
		public ProgressTask ProgressTask { get; }

		/// <summary>
		/// Gets the task ID.
		/// </summary>
		public int Id { get { return ProgressTask.Id; } }

		/// <summary>
		/// Gets or sets the task description.
		/// </summary>
		public string Description {
			get => ProgressTask.Description;
			set => ProgressTask.Description = value;
		}

		/// <summary>
		/// Gets or sets the max value of the task.
		/// </summary>
		public double MaxValue {
			get => ProgressTask.MaxValue;
			set => ProgressTask.MaxValue = value;
		}

		/// <summary>
		/// Gets or sets the value of the task.
		/// </summary>
		public double Value {
			get => ProgressTask.Value;
			set => ProgressTask.Value = Value;
		}

		/// <summary>
		/// Gets the start time of the task.
		/// </summary>
		public DateTime? StartTime {
			get => ProgressTask.StartTime;
		}

		/// <summary>
		/// Gets the stop time of the task.
		/// </summary>
		public DateTime? StopTime { get => ProgressTask.StopTime; }

		/// <summary>
		/// Gets the task state.
		/// </summary>
		public ProgressTaskState State { get => ProgressTask.State; }

		/// <summary>
		/// Gets a value indicating whether or not the task has started.
		/// </summary>
		public bool IsStarted {
			get => ProgressTask.IsStarted;
		}

		/// <summary>
		/// Gets a value indicating whether or not the task has finished.
		/// </summary>
		public bool IsFinished => StopTime != null || Value >= MaxValue;

		/// <summary>
		/// Gets the percentage done of the task.
		/// </summary>
		public double Percentage {
			get => ProgressTask.Percentage;
		}

		/// <summary>
		/// Gets the speed measured in steps/second.
		/// </summary>
		public double? Speed {
			get => ProgressTask.Speed;
		}

		/// <summary>
		/// Gets the elapsed time.
		/// </summary>
		public TimeSpan? ElapsedTime {
			get => ProgressTask.ElapsedTime;
		}

		/// <summary>
		/// Gets the remaining time.
		/// </summary>
		public TimeSpan? RemainingTime {
			get => ProgressTask.RemainingTime;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the ProgressBar shows
		/// actual values or generic, continuous progress feedback.
		/// </summary>
		public bool IsIndeterminate {
			get => ProgressTask.IsIndeterminate;
			set => ProgressTask.IsIndeterminate = value;
		}

		/// <summary>
		/// Starts the task.
		/// </summary>
		public void StartTask() {
			ProgressTask.StartTask();
		}

		/// <summary>
		/// Stops and marks the task as finished.
		/// </summary>
		public void StopTask() {
			ProgressTask.StopTask();
		}

		/// <summary>
		/// Increments the task's value.
		/// </summary>
		/// <param name="value">The value to increment with.</param>
		public void Increment(double value) {
			ProgressTask.Increment(value);
		}

		public void IncrementDouble(string key, double value) {
			Increment(key, value);
		}

		public void IncrementInt(string key, int value) {
			Increment(key, value);
		}

		public T Increment<T>(string key, T value) where T : struct, INumber<T> {
			T val = ProgressTask.State.Get<T>(key);
			return Update<T>(key, previousValue => previousValue + value);
		}

		public T Increment<T>(CounterColumn<T> counterColumn) where T : struct, INumber<T> {
			return Increment(counterColumn, counterColumn.IncrementSize);
		}

		public T Increment<T>(CounterColumn<T> counterColumn, T incrementValue) where T : struct, INumber<T> {
			var key = counterColumn.Key;
			return Increment(key, incrementValue);
		}

		public T Update<T>(string key, Func<T, T> func) where T : struct {
			return ProgressTask.State.Update(key, func);
		}
	}
}
