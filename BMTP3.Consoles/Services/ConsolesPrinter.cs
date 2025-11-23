using BMTP3.Consoles.ConsoleCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Services;
public class ConsolesPrinter
{
	/// <summary>
	/// Prints the options model to the console.
	/// </summary>
	/// <param name="optionsModels">The options models to print.</param>
	public void PrintOptionsModel(params BaseOptionsModel[] optionsModels)
	{
		foreach(var optionsModel in optionsModels)
		{
			string header = $"{optionsModel.GetType().Name}:";
			Console.WriteLine(header);
			Console.WriteLine(new string('=', header.Length));
			foreach(var line in optionsModel.GetOptionPropertyValues())
			{
				Console.WriteLine("  " + line);
			}
		}
	}
}
