using System.CommandLine;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BMTP3.Consoles.ConsoleCommands;
public class OptionsBuilder
{
	private readonly BaseOptionsModel _model;
	private readonly Dictionary<Option, Action<ParseResult>> _optionBinders = new();

	public OptionsBuilder(BaseOptionsModel model)
	{
		ArgumentNullException.ThrowIfNull(model);
		_model = model;
	}

	public OptionsBuilder AddOption<TValue>(
		TValue property,
		Option<TValue> option,
		[CallerArgumentExpression(nameof(property))] string? inputPropertyName = null
	)
	{
		if(string.IsNullOrWhiteSpace(inputPropertyName))
		{
			throw new ArgumentException($"Property name could not be determined.", inputPropertyName);
		}
		var propertyName = inputPropertyName.Contains('.') ? inputPropertyName.Split('.').Last() : inputPropertyName;

		PropertyInfo? propertyInfo = _model.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		if(propertyInfo == null)
		{
			throw new InvalidOperationException($"Property '{propertyName}' not found on {_model.GetType().Name}.");
		} else
		if(!propertyInfo.CanWrite)
		{
			throw new InvalidOperationException($"Property '{propertyName}' is not writable.");
		} else
		if(propertyInfo.PropertyType != typeof(TValue))
		{
			throw new InvalidOperationException($"Type mismatch: property '{propertyName}' is {propertyInfo.PropertyType.Name}, option expects {typeof(TValue).Name}.");
		}

		// Test duplicate option
		if(_optionBinders.ContainsKey(option))
		{
			throw new InvalidOperationException($"Option already registered for property '{propertyName}'.");
		}

		// Test duplicate option name
		if(_optionBinders.Keys.Any(opt => opt.Name == option.Name))
		{
			throw new InvalidOperationException($"An option with the name '{option.Name}' is already registered.");
		}

		_optionBinders.Add(option, (parseResult) =>
		{
			var value = parseResult.GetValue<TValue>(option);
			propertyInfo.SetValue(_model, value);
		});

		return this;
	}

	public Dictionary<Option, Action<ParseResult>> Build()
	{
		return _optionBinders;
	}
}

