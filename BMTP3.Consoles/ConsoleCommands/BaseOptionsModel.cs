using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;

namespace BMTP3.Consoles.ConsoleCommands;

/// <summary>
///     Base class for command line options models.
///     Automatically binds static Option&lt;T&gt; properties to instance properties using naming convention.
/// </summary>
/// <remarks>
///     Convention:
///     For each public static property named {Name}Option of type Option&lt;T&gt;,
///     there must be a public instance property named {Name} of type T.
///     
///     Optional:
///     A public instance property named {Name}OptionResult of type OptionResult may be added
///     to receive the raw parse result for the option.
/// </remarks>
public abstract class BaseOptionsModel
{
	private const string OptionSuffix = "Option";
	private const string OptionResultSuffix = "OptionResult";

	private static readonly ConcurrentDictionary<Type, Lazy<ModelDefinition>> _definitions = new();

	private static readonly MethodInfo _getValueOpenMethod = ResolveGetValueMethod();

	#region Public API

	/// <summary>
	///     Applies parsed command line options to this model instance.
	/// </summary>
	public void ApplyOptions(ParseResult parseResult)
	{
		ArgumentNullException.ThrowIfNull(parseResult);

		ModelDefinition definition = GetOrBuildDefinition();

		foreach (OptionBinding binding in definition.Bindings)
		{
			object? value = binding.GetValue(parseResult);
			SetPropertyValue(binding.ValueProperty, value);

			if (binding.OptionResultProperty is { } resultProperty)
			{
				resultProperty.SetValue(this, parseResult.GetResult(binding.Option));
			}
		}

		DoValidateAndSetDefaults(parseResult);
	}

	/// <summary>
	///     Returns all options defined by this model type.
	/// </summary>
	public List<Option> GetAllOptions()
	{
		return GetOrBuildDefinition().Options.ToList();
	}

	/// <summary>
	///     Validates that no option names or aliases collide across the provided models.
	/// </summary>
	public static void ValidateDuplicateOptionNamesOrAliases(IEnumerable<BaseOptionsModel> models)
	{
		ArgumentNullException.ThrowIfNull(models);

		Dictionary<string, (Option Option, Type ModelType)> seen =
			new(StringComparer.OrdinalIgnoreCase);

		List<string> collisions = new();

		foreach (BaseOptionsModel model in models)
		{
			Type modelType = model.GetType();

			foreach (Option option in model.GetAllOptions())
			{
				foreach (string name in GetAllNames(option))
				{
					if (seen.TryGetValue(name, out (Option Option, Type ModelType) existing))
					{
						collisions.Add(
							$"  '{name}' used by [{existing.Option}] in {existing.ModelType.FullName} " +
							$"and [{option}] in {modelType.FullName}");
					}
					else
					{
						seen[name] = (option, modelType);
					}
				}
			}
		}

		if (collisions.Count > 0)
		{
			throw new InvalidOperationException(
				"Duplicate option names or aliases detected:" + Environment.NewLine +
				string.Join(Environment.NewLine, collisions));
		}
	}

	/// <summary>
	///     Returns current option/property values as formatted diagnostic lines.
	/// </summary>
	public IReadOnlyList<string> GetOptionPropertyValues()
	{
		ModelDefinition definition = GetOrBuildDefinition();

		var rows = definition.Bindings.Select(binding => new
		{
			Option = binding.Option.Name,
			Property = binding.ValueProperty.Name,
			Type = FormatTypeName(binding.ValueProperty.PropertyType),
			Value = FormatValue(binding.ValueProperty.GetValue(this))
		}).ToList();

		const string hOption = "Option";
		const string hProperty = "Property";
		const string hType = "Type";
		const string hValue = "Value";

		int optionWidth = Math.Max(
			hOption.Length,
			rows.Select(row => row.Option.Length).DefaultIfEmpty(0).Max());

		int propertyWidth = Math.Max(
			hProperty.Length,
			rows.Select(row => row.Property.Length).DefaultIfEmpty(0).Max());

		int typeWidth = Math.Max(
			hType.Length,
			rows.Select(row => row.Type.Length).DefaultIfEmpty(0).Max());

		string FormatRow(string option, string property, string type, string value)
		{
			return $"{option.PadRight(optionWidth)}  {property.PadRight(propertyWidth)}  {type.PadRight(typeWidth)}  {value}";
		}

		List<string> result = new(rows.Count + 2)
		{
			FormatRow(hOption, hProperty, hType, hValue),
			FormatRow(
				new string('-', hOption.Length),
				new string('-', hProperty.Length),
				new string('-', hType.Length),
				new string('-', hValue.Length))
		};

		foreach (var row in rows)
		{
			result.Add(FormatRow(row.Option, row.Property, row.Type, row.Value));
		}

		return result;
	}

	#endregion

	#region Extension points

	/// <summary>
	///     Override to add validators to this model's static options.
	///     Executed once per model type when the definition is first built.
	/// </summary>
	protected virtual void DoAddValidators()
	{
	}

	/// <summary>
	///     Override to validate populated values and/or set derived defaults after parsing.
	/// </summary>
	protected virtual void DoValidateAndSetDefaults(ParseResult result)
	{
	}

	#endregion

	#region Definition building

	private ModelDefinition GetOrBuildDefinition()
	{
		return _definitions.GetOrAdd(
			GetType(),
			type => new Lazy<ModelDefinition>(
				() =>
				{
					ModelDefinition definition = BuildDefinition(type);
					DoAddValidators();
					return definition;
				},
				LazyThreadSafetyMode.ExecutionAndPublication)
		).Value;
	}

	private static ModelDefinition BuildDefinition(Type modelType)
	{
		List<PropertyInfo> optionProperties = modelType
			.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			.Where(property => property.Name.EndsWith(OptionSuffix, StringComparison.Ordinal))
			.Where(property => typeof(Option).IsAssignableFrom(property.PropertyType))
			.OrderBy(property => property.MetadataToken)
			.ToList();

		Dictionary<string, PropertyInfo> instanceProperties = modelType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(property => property.CanRead)
			.ToDictionary(
				property => property.Name,
				property => property,
				StringComparer.OrdinalIgnoreCase);

		List<OptionBinding> bindings = new();

		foreach (PropertyInfo optionProperty in optionProperties)
		{
			if (!TryGetOptionValueType(optionProperty.PropertyType, out Type? valueType))
			{
				throw new InvalidOperationException(
					$"Static option property '{optionProperty.Name}' must be of type Option<T>.");
			}

			ArgumentNullException.ThrowIfNull(valueType);
			OptionBinding binding = CreateBinding(optionProperty, valueType, instanceProperties);
			bindings.Add(binding);
		}

		ValidateNoDuplicateOptionInstances(modelType, bindings);

		return new ModelDefinition(bindings);
	}

	private static OptionBinding CreateBinding(
		PropertyInfo optionProperty,
		Type valueType,
		Dictionary<string, PropertyInfo> instanceProperties)
	{
		string baseName = optionProperty.Name[..^OptionSuffix.Length];

		if (!instanceProperties.TryGetValue(baseName, out PropertyInfo? valueProperty))
		{
			throw new InvalidOperationException(
				$"No instance property '{baseName}' found for static option '{optionProperty.Name}'.");
		}

		if (!valueProperty.CanWrite)
		{
			throw new InvalidOperationException(
				$"Instance property '{valueProperty.Name}' must have a setter.");
		}

		if (valueProperty.PropertyType != valueType)
		{
			throw new InvalidOperationException(
				$"Type mismatch: '{optionProperty.Name}' is Option<{valueType.Name}> " +
				$"but '{valueProperty.Name}' is {valueProperty.PropertyType.Name}.");
		}

		PropertyInfo? optionResultProperty = ResolveOptionResultProperty(
			baseName,
			instanceProperties);

		Option option = optionProperty.GetValue(null) as Option
			?? throw new InvalidOperationException(
				$"Option '{optionProperty.Name}' returned null.");

		if (!optionProperty.PropertyType.IsInstanceOfType(option))
		{
			throw new InvalidOperationException(
				$"Option instance for '{optionProperty.Name}' is not of expected type '{optionProperty.PropertyType.Name}'. " +
				$"Actual type: '{option.GetType().Name}'.");
		}

		MethodInfo getValueMethod = _getValueOpenMethod.MakeGenericMethod(valueType);

		return new OptionBinding(
			option,
			valueProperty,
			optionResultProperty,
			getValueMethod);
	}

	private static PropertyInfo? ResolveOptionResultProperty(
		string baseName,
		Dictionary<string, PropertyInfo> instanceProperties)
	{
		string optionResultPropertyName = baseName + OptionResultSuffix;

		if (!instanceProperties.TryGetValue(optionResultPropertyName, out PropertyInfo? optionResultProperty))
		{
			return null;
		}

		if (optionResultProperty.PropertyType != typeof(OptionResult))
		{
			throw new InvalidOperationException(
				$"'{optionResultProperty.Name}' must be of type {nameof(OptionResult)}.");
		}

		if (!optionResultProperty.CanWrite)
		{
			throw new InvalidOperationException(
				$"'{optionResultProperty.Name}' must have a setter.");
		}

		return optionResultProperty;
	}

	private static void ValidateNoDuplicateOptionInstances(
		Type modelType,
		IReadOnlyList<OptionBinding> bindings)
	{
		List<string> duplicates = bindings
			.GroupBy(binding => binding.Option)
			.Where(group => group.Count() > 1)
			.Select(group =>
				$"  Option '{group.Key}' bound to: " +
				string.Join(", ", group.Select(binding => binding.ValueProperty.Name)))
			.ToList();

		if (duplicates.Count > 0)
		{
			throw new InvalidOperationException(
				$"Duplicate option instances in {modelType.FullName}:" + Environment.NewLine +
				string.Join(Environment.NewLine, duplicates));
		}
	}

	#endregion

	#region Helpers

	private void SetPropertyValue(PropertyInfo property, object? value)
	{
		if (value is null &&
			property.PropertyType.IsValueType &&
			Nullable.GetUnderlyingType(property.PropertyType) is null)
		{
			return;
		}

		if (value is not null)
		{
			Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

			if (!targetType.IsInstanceOfType(value))
			{
				throw new InvalidOperationException(
					$"Value of type '{value.GetType().Name}' is not assignable to " +
					$"'{property.Name}' of type '{property.PropertyType.Name}'.");
			}
		}

		property.SetValue(this, value);
	}

	private static IEnumerable<string> GetAllNames(Option option)
	{
		yield return option.Name;

		foreach (string alias in option.Aliases)
		{
			yield return alias;
		}
	}

	private static bool TryGetOptionValueType(Type type, out Type? valueType)
	{
		for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
		{
			if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Option<>))
			{
				valueType = current.GetGenericArguments()[0];
				return true;
			}
		}

		valueType = null;
		return false;
	}

	private static MethodInfo ResolveGetValueMethod()
	{
		MethodInfo? method = typeof(ParseResult)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(method =>
			{
				if (method.Name != nameof(ParseResult.GetValue))
				{
					return false;
				}

				if (!method.IsGenericMethodDefinition)
				{
					return false;
				}

				ParameterInfo[] parameters = method.GetParameters();

				if (parameters.Length != 1)
				{
					return false;
				}

				Type parameterType = parameters[0].ParameterType;

				return parameterType.IsGenericType &&
					   parameterType.GetGenericTypeDefinition() == typeof(Option<>);
			});

		if (method is null)
		{
			throw new InvalidOperationException(
				$"Could not find generic {nameof(ParseResult.GetValue)}<T>(Option<T>) method on {nameof(ParseResult)}.");
		}

		return method;
	}

	private static string FormatTypeName(Type type)
	{
		if (Nullable.GetUnderlyingType(type) is { } underlyingType)
		{
			return FormatTypeName(underlyingType) + "?";
		}

		if (!type.IsGenericType)
		{
			return type.Name;
		}

		string name = type.Name;
		int tickIndex = name.IndexOf('`');

		if (tickIndex >= 0)
		{
			name = name[..tickIndex];
		}

		string arguments = string.Join(
			", ",
			type.GetGenericArguments().Select(FormatTypeName));

		return $"{name}<{arguments}>";
	}

	private static string FormatValue(object? value)
	{
		return value switch
		{
			null => "(null)",
			string text => $"\"{text}\"",
			FileSystemInfo fileSystemInfo => $"\"{fileSystemInfo}\"",
			IEnumerable<string> items => "[" + string.Join(", ", items.Select(item => $"\"{item}\"")) + "]",
			_ => value.ToString() ?? "(null)"
		};
	}

	#endregion

	#region Inner types

	private sealed class ModelDefinition
	{
		public IReadOnlyList<OptionBinding> Bindings { get; }
		public IReadOnlyList<Option> Options { get; }

		public ModelDefinition(IReadOnlyList<OptionBinding> bindings)
		{
			Bindings = bindings;
			Options = bindings.Select(binding => binding.Option).ToList();
		}
	}

	private sealed record OptionBinding(
		Option Option,
		PropertyInfo ValueProperty,
		PropertyInfo? OptionResultProperty,
		MethodInfo GetValueMethod)
	{
		public object? GetValue(ParseResult parseResult)
		{
			return GetValueMethod.Invoke(parseResult, new object[] { Option });
		}
	}

	#endregion
}