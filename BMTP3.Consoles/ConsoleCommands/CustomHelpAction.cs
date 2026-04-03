using System.CommandLine;
using System.CommandLine.Invocation;

namespace BMTP3.Consoles.ConsoleCommands;
public sealed class CustomHelpAction : SynchronousCommandLineAction
{
	public override int Invoke(ParseResult parseResult)
	{
		TextWriter output = parseResult.InvocationConfiguration.Output;
		Command command = parseResult.CommandResult.Command;

		// Description
		if(!string.IsNullOrEmpty(command.Description))
		{
			output.WriteLine(command.Description);
			output.WriteLine();
		}

		// Options  
		if(command.Options.Count > 0)
		{
			output.WriteLine("Options:");
			foreach(Option option in command.Options)
			{
				if(option.Hidden) continue;

				// Build alias string: -c, --config  
				List<string> aliases = new List<string> { option.Name };
				foreach(string alias in option.Aliases)
				{
					aliases.Add(alias);
				}

				aliases.Sort((a, b) => a.Length.CompareTo(b.Length));
				string aliasText = string.Join(", ", aliases);
				// Build argument label with brackets if optional  
				string argLabel = GetArgumentLabel(option);
				if(!string.IsNullOrEmpty(argLabel))
				{
					aliasText += $" {argLabel}";
				}

				string description = option.Description ?? "";
				output.WriteLine($"  {aliasText,-40} {description}");
			}
		}

		return 0;
	}

	private static string GetArgumentLabel(Option option)
	{
		// No argument to show  
		if(option.Arity.MaximumNumberOfValues == 0)
		{
			return "";
		}

		string name = option.HelpName
					  ?? option.Name.TrimStart('-', '/');

		bool isOptional = option.Arity.MinimumNumberOfValues == 0 && option.Arity.MaximumNumberOfValues >= 1;

		return isOptional
			? $"[<{name}>]"    // <-- this adds the brackets you want  
			: $"<{name}>";
	}
}