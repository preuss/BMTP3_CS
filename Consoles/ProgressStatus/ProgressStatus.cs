using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public class ProgressStatus {
		private IAnsiConsole _console;
		public string Description { get; set; }
		public double Progress { get; set; }
		public string Status { get; set; }
		private ProgressStatus(IAnsiConsole? console, string description) {
			_console = console ?? throw new ArgumentNullException("console");
			Description = description;
			Progress = 0;
			Status = "Not Started";
		}

		//
		// Summary:
		//     Initializes a new instance of the Spectre.Console.Status class.
		//
		// Parameters:
		//   console:
		//     The console.
		public ProgressStatus(IAnsiConsole console) : this(console, "") {
		}

		public ProgressStatus(string description) : this(AnsiConsole.Console, description) {
		}

		public void UpdateProgress(double value) {
			Progress = value;
		}

		public void UpdateStatus(string status) {
			Status = status;
		}
		public void RenderTasks(List<ProgressStatus> tasks) {
			Console.Clear();
			foreach(var task in tasks) {
				Console.WriteLine($"{task.Description}: {task.Progress}% - {task.Status}");
			}
		}
		public async Task RunTasksAsync(List<ProgressStatus> tasks) {
			await AnsiConsole.Live(new Panel("Progress Status"))
				.StartAsync(async ctx => {
					while(tasks.Any(t => t.Progress < 100)) {
						foreach(var task in tasks) {
							if(task.Progress < 100) {
								task.UpdateProgress(task.Progress + 1);
								task.UpdateStatus("In Progress");
							} else {
								task.UpdateStatus("Completed");
							}
						}

						// Opdater visningen
						var table = new Table();
						table.AddColumn("Description");
						table.AddColumn("Progress");
						table.AddColumn("Status");

						foreach(var task in tasks) {
							table.AddRow(task.Description, $"{task.Progress}%", task.Status);
						}

						ctx.UpdateTarget(table);
						await Task.Delay(100); // Vent lidt før næste opdatering
					}
				});
		}
		/*
		public async Task RunTasksAsync(List<ProgressStatus> tasks)
		{
			while (tasks.Any(t => t.Progress < 100))
			{
				foreach (var task in tasks)
				{
					if (task.Progress < 100)
					{
						task.UpdateProgress(task.Progress + 1);
						task.UpdateStatus("In Progress");
					}
					else
					{
						task.UpdateStatus("Completed");
					}
				}

				RenderTasks(tasks);
				await Task.Delay(100); // Vent lidt før næste opdatering
			}
		}
		*/
	}
}
