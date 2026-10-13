using System.Collections;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BMTP3.Consoles.ConsoleCommands;

// Help renderer following System.CommandLine 3.x layout (Description:, dynamic columns, wrapping).
// Two toggles (3.x offers neither on 2.x):
//   BracketedArgumentLabels - true: "-c, --config [<config>]" / bools get a label; false: 3.x style "-c, --config <config>" / bools get no label.
//   ShowParentOptions       - true: 3.x style, parent recursive (global) options shown for subcommands; false: only the command's own options.
public sealed class CustomHelpAction : SynchronousCommandLineAction
{
	private const string Indent = "  ";

	public bool BracketedArgumentLabels { get; init; } = true;

	public bool ShowParentOptions { get; init; } = true;

	public override int Invoke(ParseResult parseResult)
	{
		TextWriter output = parseResult.InvocationConfiguration.Output;
		Command command = parseResult.CommandResult.Command;

		int maxWidth = GetMaxWidth();

		// Description
		if (!string.IsNullOrEmpty(command.Description))
		{
			output.WriteLine("Description:");
			foreach (string part in WrapText(command.Description, maxWidth - Indent.Length))
			{
				output.Write(Indent);
				output.WriteLine(part);
			}
			output.WriteLine();
		}

		// Usage
		output.WriteLine("Usage:");
		output.Write(Indent);
		output.WriteLine(BuildUsage(command));
		output.WriteLine();

		// Options
		List<(string First, string Second)> optionRows = GetOptionRows(command);
		if (optionRows.Count > 0)
		{
			output.WriteLine("Options:");
			WriteColumns(optionRows, output, maxWidth);
			output.WriteLine();
		}

		// Commands
		List<(string First, string Second)> commandRows = GetCommandRows(command);
		if (commandRows.Count > 0)
		{
			output.WriteLine("Commands:");
			WriteColumns(commandRows, output, maxWidth);
			output.WriteLine();
		}

		return 0;
	}

	private static int GetMaxWidth()
	{
		try
		{
			return Console.IsOutputRedirected ? int.MaxValue : Console.WindowWidth;
		}
		catch (Exception)
		{
			return int.MaxValue;
		}
	}

	private static string BuildUsage(Command command)
	{
		List<string> parts = new();

		List<Command> chain = new();
		Command? current = command;
		while (current is not null)
		{
			chain.Add(current);
			current = current.Parents.OfType<Command>().FirstOrDefault();
		}
		chain.Reverse();

		bool displayOptionTitle = false;

		foreach (Command parentCommand in chain)
		{
			if (!displayOptionTitle)
			{
				displayOptionTitle = parentCommand.Options.Any(x => x.Recursive && !x.Hidden);
			}

			parts.Add(parentCommand.Name);

			string argumentUsage = string.Join(" ", parentCommand.Arguments
				.Where(a => !a.Hidden)
				.Select(a => a.Arity.MinimumNumberOfValues == 0 ? $"[<{a.Name}>]" : $"<{a.Name}>"));

			if (!string.IsNullOrEmpty(argumentUsage))
			{
				parts.Add(argumentUsage);
			}
		}

		if (command.Subcommands.Any(x => !x.Hidden))
		{
			parts.Add("[command]");
		}

		displayOptionTitle = displayOptionTitle || command.Options.Any(x => !x.Hidden);

		if (displayOptionTitle)
		{
			parts.Add("[options]");
		}

		return string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
	}

	private List<(string First, string Second)> GetOptionRows(Command command)
	{
		List<(string First, string Second)> rows = new();
		bool addedHelpOption = false;

		foreach (Option option in command.Options.OrderBy(o => o is HelpOption or VersionOption))
		{
			if (option.Hidden)
			{
				continue;
			}

			if (option is HelpOption)
			{
				addedHelpOption = true;
			}

			rows.Add(GetOptionRow(option));
		}

		if (ShowParentOptions)
		{
			Command? current = command;
			while (current is not null)
			{
				Command? parentCommand = null;
				foreach (Symbol parent in current.Parents)
				{
					if ((parentCommand = parent as Command) is not null)
					{
						foreach (Option option in parentCommand.Options)
						{
							// global help aliases may be duplicated, we just ignore them
							if (option is { Recursive: true, Hidden: false })
							{
								if (option is not HelpOption || !addedHelpOption)
								{
									if (option is HelpOption)
									{
										addedHelpOption = true;
									}

									rows.Add(GetOptionRow(option));
								}
							}
						}

						break;
					}
				}
				current = parentCommand;
			}
		}

		return rows;
	}

	private (string First, string Second) GetOptionRow(Option option)
	{
		string firstColumnText = GetIdentifierLabel(option.Name, option.Aliases);

		string argumentFirstColumnText = GetArgumentLabel(option);
		if (!string.IsNullOrEmpty(argumentFirstColumnText))
		{
			firstColumnText += $" {argumentFirstColumnText}";
		}

		if (option.Required)
		{
			firstColumnText += " (REQUIRED)";
		}

		string secondColumnText = $"{option.Description ?? ""}{GetDefaultSuffix(option)}".Trim();

		return (firstColumnText, secondColumnText);
	}

	private static List<(string First, string Second)> GetCommandRows(Command command)
	{
		List<(string First, string Second)> rows = new();

		foreach (Command sub in command.Subcommands)
		{
			if (sub.Hidden)
			{
				continue;
			}

			rows.Add((GetIdentifierLabel(sub.Name, sub.Aliases), sub.Description ?? ""));
		}

		return rows;
	}

	private static string GetIdentifierLabel(string name, ICollection<string> aliases)
	{
		IEnumerable<string> aliasParts = new[] { name }
			.Concat(aliases)
			.Select(SplitPrefix)
			.OrderBy(r => r.Prefix, StringComparer.OrdinalIgnoreCase)
			.ThenBy(r => r.Alias, StringComparer.OrdinalIgnoreCase)
			.GroupBy(t => t.Alias)
			.Select(t => t.First())
			.Select(t => $"{t.Prefix}{t.Alias}");

		return string.Join(", ", aliasParts);
	}

	private static (string Prefix, string Alias) SplitPrefix(string alias)
	{
		if (alias.StartsWith("--", StringComparison.Ordinal))
		{
			return ("--", alias[2..]);
		}

		if (alias.StartsWith('-') || alias.StartsWith('/'))
		{
			return (alias[..1], alias[1..]);
		}

		return ("", alias);
	}

	private string GetArgumentLabel(Option option)
	{
		// 3.x parity with toggle off: bool options are flags, no label at all.
		if (!BracketedArgumentLabels && (option.ValueType == typeof(bool) || option.ValueType == typeof(bool?)))
		{
			return "";
		}

		// No argument to show
		if (option.Arity.MaximumNumberOfValues == 0)
		{
			return "";
		}

		string name = option.HelpName
					  ?? option.Name.TrimStart('-', '/');

		if (!BracketedArgumentLabels)
		{
			return $"<{name}>";
		}

		bool isOptional = option.Arity.MinimumNumberOfValues == 0 && option.Arity.MaximumNumberOfValues >= 1;

		return isOptional
			? $"[<{name}>]"
			: $"<{name}>";
	}

	private static string GetDefaultSuffix(Option option)
	{
		object? defaultValue = GetDefaultValueOrNull(option);

		string rendered = defaultValue switch
		{
			bool flag => flag ? "true" : "",
			null => "",
			string str => str,
			IEnumerable enumerable => string.Join("|", enumerable.Cast<object>()),
			_ => defaultValue.ToString() ?? ""
		};

		return rendered.Length == 0 ? "" : $" [default: {rendered}]";
	}

	private static object? GetDefaultValueOrNull(Option option)
	{
		try
		{
			return option.GetDefaultValue();
		}
		catch(InvalidOperationException)
		{
			return null;
		}
	}

	private static void WriteColumns(List<(string First, string Second)> rows, TextWriter output, int maxWidth)
	{
		if (rows.Count == 0)
		{
			return;
		}

		int firstColumnWidth = rows.Max(r => r.First.Length);
		int secondColumnWidth = rows.Max(r => r.Second.Length);

		if (firstColumnWidth + secondColumnWidth + Indent.Length + Indent.Length > maxWidth)
		{
			int firstColumnMaxWidth = maxWidth / 2 - Indent.Length;
			if (firstColumnWidth > firstColumnMaxWidth)
			{
				firstColumnWidth = rows.SelectMany(r => WrapText(r.First, firstColumnMaxWidth).Select(l => l.Length)).Max();
			}
			secondColumnWidth = maxWidth - firstColumnWidth - Indent.Length - Indent.Length;
		}

		foreach ((string first, string second) in rows)
		{
			IEnumerable<string> firstColumnParts = WrapText(first, firstColumnWidth);
			IEnumerable<string> secondColumnParts = WrapText(second, secondColumnWidth);

			using IEnumerator<string> enum1 = firstColumnParts.GetEnumerator();
			using IEnumerator<string> enum2 = secondColumnParts.GetEnumerator();
			bool hasFirst = false, hasSecond = false;

			while ((hasFirst = enum1.MoveNext()) | (hasSecond = enum2.MoveNext()))
			{
				string firstPart = hasFirst ? enum1.Current : "";
				string secondPart = hasSecond ? enum2.Current : "";

				output.Write($"{Indent}{firstPart}");

				if (!string.IsNullOrWhiteSpace(secondPart))
				{
					int padSize = firstColumnWidth - firstPart.Length;
					if (padSize > 0)
					{
						output.Write(new string(' ', padSize));
					}

					output.Write($"{Indent}{secondPart}");
				}

				output.WriteLine();
			}
		}
	}

	private static IEnumerable<string> WrapText(string text, int maxWidth)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			yield break;
		}

		string[] parts = text.Split(["\r\n", "\n"], StringSplitOptions.None);

		foreach (string part in parts)
		{
			if (part.Length <= maxWidth)
			{
				yield return part;
			}
			else
			{
				for (int i = 0; i < part.Length;)
				{
					if (part.Length - i < maxWidth)
					{
						yield return part.Substring(i);
						break;
					}

					int length = -1;
					for (int j = 0; j + i < part.Length && j < maxWidth; j++)
					{
						if (char.IsWhiteSpace(part[i + j]))
						{
							length = j + 1;
						}
					}
					if (length == -1)
					{
						length = maxWidth;
					}
					yield return part.Substring(i, length);

					i += length;
				}
			}
		}
	}
}
