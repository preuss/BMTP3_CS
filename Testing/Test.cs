using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Testing {
	internal class Test {
		public static void Main_(string[] args) {
			Console.WriteLine("Hello World!");
			//ProgressTwo();
			//LiveTwo();
			LiveTable();
		}

		public static void LiveTable() {
			var task1Progress = 0;
			var task2Progress = 0;

			var table = new Table().Centered();

			AnsiConsole.Live(table)
				.Start(ctx => {
					table.AddColumn("Foo");
					ctx.Refresh();
					Thread.Sleep(1000);

					table.AddColumn("Bar");
					ctx.Refresh();
					Thread.Sleep(1000);

					// Tilføj en initial række til tabellen
					table.AddRow("[green]Opgave 1: 0%[/]", "[blue]Opgave 2: 0 enheder[/]");

					int i = 0;
					while(i++ < 1000) {
						task1Progress++;
						task2Progress++;

						// Opdater indholdet af den eksisterende række
						table.Rows.Update(0, 0, new Markup($"[green]Opgave 1: {task1Progress}%[/]"));
						table.Rows.Update(0, 1, new Markup($"[blue]Opgave 2: {task2Progress} enheder[/]"));

						ctx.Refresh();
						Thread.Sleep(2);
					}
				});
		}

		public static void LiveTwo() {

			//var table = new Panel("Live Display");
			var table = new Table() { Border = TableBorder.None,ShowHeaders = false };

			var task1Progress = 0;
			var task2Progress = 0;

			AnsiConsole.Live(table)
				.Start(ctx => {
					while(task1Progress < 100 || task2Progress < 100) {
						// Opdater task1
						if(task1Progress < 100) {
							task1Progress += 1;
						}

						// Opdater task2
						if(task2Progress < 100) {
							task2Progress += 2;
						}
						table.AddColumn("Hello world");
						ctx.Refresh();
						/*
						// Opdater display
						ctx.UpdateTarget(new Panel(
							new Rows(
								new Markup($"[green]Opgave 1: {task1Progress}%[/]"),
								new Markup($"[blue]Opgave 2: {task2Progress} enheder[/]")
							)
						) { Border = BoxBorder.None });
						*/

						// Vent lidt før næste opdatering
						Thread.Sleep(25);
					}
				});
		}

		public static void ProgressTwo() {
			Console.WriteLine("Progress two lines");
			AnsiConsole.Progress()
			.Start(ctx => {
				// Definer opgaver
				var task1 = ctx.AddTask("[green]Opgave 1[/]");
				var task2 = ctx.AddTask("[green]Opgave 2[/]");

				while(!ctx.IsFinished) {
					// Simuler noget arbejde
					Thread.Sleep(250);

					// Opdater fremdrift
					task1.Increment(1.5);
					task2.Increment(0.5);
				}
			});
		}
	}
}
