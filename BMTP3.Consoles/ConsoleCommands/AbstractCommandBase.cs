using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public abstract class AbstractCommandBase(string name, string? description = null) : Command(name, description) {
	protected OptionResult? GetOptionResult(ParseResult parseResult, string optionName) {

		/*
		// Only finds the optionResults in this parseResult.
		OptionResult? optionResult = parseResult.CommandResult.Children
			.OfType<OptionResult>()
			.FirstOrDefault(optionResult => optionResult.Option.Name == optionName);
		*/
		// Looks in root and all parseResults 
		if(parseResult.GetResult(optionName) is OptionResult optionResult) {
			return optionResult;
		}
		return null;
	}

	public static T PopulateOptions<T>(ParseResult parseResult, IEnumerable<Option> options) where T : new() {
		List<Option> optionList = options?.ToList() ?? [];

		T optionsObject = new T();
		Type type = typeof(T);
		foreach(var prop in type.GetProperties()) {
			var option = optionList.FirstOrDefault(o => OptionNameMatchesProperty(o.Name, prop.Name));
			if(option == null) {
				throw new InvalidOperationException($"No option found for property '{prop.Name}'");
			}

			Type optionType = typeof(Option<>).MakeGenericType(prop.PropertyType);
			if(!optionType.IsInstanceOfType(option)) {
				throw new InvalidOperationException(
					$"Option '{option.Name}' does not match property type '{prop.PropertyType.Name}'");
			}

			var method = typeof(ParseResult).GetMethod("GetValue")!.MakeGenericMethod(prop.PropertyType);
			var value = method.Invoke(parseResult, new object[] { option });
			prop.SetValue(optionsObject, value);
		}
		return optionsObject;
	}
	private static bool OptionNameMatchesProperty(string optionName, string propertyName) {
		var opt = optionName.TrimStart('-');
		if(string.IsNullOrWhiteSpace(opt) || string.IsNullOrWhiteSpace(propertyName)) {
			return false;
		}
		if(opt.Length != propertyName.Length) {
			return false;
		}

		if(opt[0] != char.ToLowerInvariant(propertyName[0])) {
			return false;
		}

		if(opt.Length == 1) {
			return true;
		}

		return opt.Substring(1).Equals(propertyName.Substring(1), StringComparison.Ordinal);
	}
}