using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;

namespace BMTP3.Consoles.ConsoleCommands;

/// <summary>
///     Base class for command line options models.
///     Automatically binds static Option{T} properties to instance properties using naming convention.
/// </summary>
/// <remarks>
///     Convention: For each static property named {Name}Option of type Option{T},
///     there must be an instance property named {Name} of type T.
/// </remarks>
public abstract class BaseOptionsModel
{
	private static readonly ConcurrentDictionary<Type, Dictionary<Option, Action<ParseResult>>> _optionBindersCache =
		new();

	/// <summary>
	///     Gets or creates the option binders for this model type.
	/// </summary>
	/// <returns>A dictionary mapping options to actions that bind their values to instance properties.</returns>
	private Dictionary<Option, Action<ParseResult>> GetOrCreateOptionBinders()
	{
		// TODO: Add thread safe caching
		Type type = GetType();
		// Use GetOrAdd to guarantee thread-safe lazy initialization
		return _optionBindersCache.GetOrAdd(type, t =>
		{
			Dictionary<Option, Action<ParseResult>> binders = DoDefineOptions();
			DoAddValidators();
			return binders;
		});
	}

	/// <summary>
	///     Validates that there are no duplicate option names or aliases across the provided models.
	///     Throws an exception if duplicates are found.
	/// </summary>
	/// <param name="models">The option models to validate.</param>
	public static void ValidateDuplicateNameAndAlias(IEnumerable<BaseOptionsModel> models)
	{
		// Collect all option names and aliases, with references to their Option and model Type
		var allNames = models
			.SelectMany(optionsModel => optionsModel.GetAllOptions().Select(option =>
				new
				{
					Names = new[] { option.Name }.Concat(option.Aliases),
					Option = option,
					ModelType = optionsModel.GetType()
				}))
			.SelectMany(x => x.Names.Select(name => new { Name = name, x.Option, x.ModelType }))
			.ToList();

		// Find duplicates (collisions)
		var duplicates = allNames
			.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1)
			.ToList();

		if (duplicates.Any())
		{
			string msg = string.Join(
				Environment.NewLine,
				duplicates.Select(g =>
					$"Collision for name/alias '{g.Key}':" + Environment.NewLine +
					string.Join(Environment.NewLine, g.Select(x =>
						$"  Option: {x.Option}, Model: {x.ModelType.FullName}"
					))
				)
			);
			throw new InvalidOperationException("Duplicate option names or aliases detected:" + Environment.NewLine +
			                                    msg);
		}
	}

	/// <summary>
	///     Defines the options and their binders for this model type.
	/// </summary>
	/// <returns>A dictionary mapping options to actions that bind their values to instance properties.</returns>
	protected virtual Dictionary<Option, Action<ParseResult>> DoDefineOptions()
	{
		Dictionary<Option, Action<ParseResult>> dict = new();

		Type type = GetType();
		List<PropertyInfo> optionProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType))
			.Where(p => p.Name.EndsWith("Option", StringComparison.Ordinal))
			.Where(p => IsGenericTypeAssignableFrom(p.PropertyType, typeof(Option<>)))
			.ToList();

		Dictionary<string, PropertyInfo> instanceProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		foreach (PropertyInfo optionProp in optionProps)
		{
			string baseName = ExtractOptionBaseName(optionProp.Name);
			if (!instanceProps.TryGetValue(baseName, out PropertyInfo? instanceProp))
			{
				throw new InvalidOperationException(
					$"The corresponding instance property {baseName} does not exist for {optionProp.Name}");
			}

			// Find the opt-in *OptionResult property (e.g. "ConfigOptionResult")
			instanceProps.TryGetValue(baseName + "OptionResult", out PropertyInfo? optionResultProp);
			if (optionResultProp != null && optionResultProp.PropertyType != typeof(OptionResult))
			{
				throw new InvalidOperationException(
					$"Type mismatch: {optionResultProp.Name} must be of type OptionResult");
			}

			Option? optionInstance = (Option?)optionProp.GetValue(null);
			ArgumentNullException.ThrowIfNull(optionInstance);

			dict.Add(optionInstance,
				parseResult =>
					BindOptionPropertyFromParseResult(parseResult, optionProp, instanceProp, optionResultProp));
		}

		return dict;
	}

	/// <summary>
	///     Extracts the base property name from a static option property name.
	/// </summary>
	/// <param name="optionPropertyName">The static option property name.</param>
	/// <returns>The base property name.</returns>
	private static string ExtractOptionBaseName(string optionPropertyName)
	{
		return optionPropertyName.Substring(0, optionPropertyName.Length - "Option".Length);
	}

	/// <summary>
	///     Applies command line options from the given <see cref="ParseResult" /> to this model instance.
	///     Calls <see cref="DoPopulate" /> and <see cref="DoValidateAndSetDefaults" /> in sequence.
	/// </summary>
	/// <param name="parseResult">The parsed command line result.</param>
	public void ApplyOptions(ParseResult parseResult)
	{
		DoPopulate(parseResult);
		DoValidateAndSetDefaults(parseResult);
	}

	/// <summary>
	///     Populates this model's properties from the provided <see cref="ParseResult" />.
	///     Binds values from static Option{T} properties to corresponding instance properties.
	/// </summary>
	/// <param name="parseResult">The parsed command line result.</param>
	protected virtual void DoPopulate(ParseResult parseResult)
	{
		Dictionary<Option, Action<ParseResult>> optionBinders = GetOrCreateOptionBinders();
		foreach (Action<ParseResult> binder in optionBinders.Values)
		{
			binder(parseResult);
		}
	}

	/// <summary>
	///     Gets the generic argument type from a candidate type if it matches the generic type definition.
	/// </summary>
	/// <param name="candidate">The candidate type.</param>
	/// <param name="genericTypeDefinition">The generic type definition.</param>
	/// <returns>The generic argument type, or null if not found.</returns>
	private static Type? GetGenericType(Type? candidate, Type genericTypeDefinition)
	{
		while (candidate != null && candidate != typeof(object))
		{
			if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericTypeDefinition)
			{
				return candidate.GetGenericArguments()[0];
			}

			candidate = candidate.BaseType!;
		}

		return null;
	}

	/// <summary>
	///     Checks if a candidate type is assignable from a generic type definition.
	/// </summary>
	/// <param name="candidate">The candidate type.</param>
	/// <param name="genericTypeDefinition">The generic type definition.</param>
	/// <returns>True if assignable, otherwise false.</returns>
	private static bool IsGenericTypeAssignableFrom(Type? candidate, Type genericTypeDefinition)
	{
		return GetGenericType(candidate, genericTypeDefinition) != null;
	}

	/// <summary>
	///     Binds the value of an option from a <see cref="ParseResult" /> to an instance property.
	/// </summary>
	/// <param name="parseResult">The parse result.</param>
	/// <param name="optionProp">The static option property.</param>
	/// <param name="instanceProp">The instance property.</param>
	/// <param name="optionResultProp">The optional OptionResult property.</param>
	private void BindOptionPropertyFromParseResult(ParseResult parseResult, PropertyInfo optionProp,
		PropertyInfo instanceProp, PropertyInfo? optionResultProp)
	{
		Type optionType = optionProp.PropertyType;
		Type? optionArgumentType = GetGenericType(optionType, typeof(Option<>));
		if (optionArgumentType == null)
		{
			throw new InvalidOperationException($"{optionProp.Name} is not an Option<T>");
		}

		if (instanceProp.PropertyType != optionArgumentType)
		{
			throw new InvalidOperationException(
				$"Type mismatch: {optionProp.Name} is Option<{optionArgumentType.Name}>, but {instanceProp.Name} is {instanceProp.PropertyType.Name}");
		}

		if (optionProp.GetValue(null) is not Option optionInstance)
		{
			throw new InvalidOperationException($"Option instance for {optionProp.Name} is not an Option");
		}

		if (!optionType.IsInstanceOfType(optionInstance))
		{
			throw new InvalidOperationException(
				$"Option instance for {optionProp.Name} is not of type Option<{optionArgumentType.Name}>. Actual type: {optionInstance.GetType()}");
		}

		MethodInfo? getValueMethod = typeof(ParseResult)
			.GetMethods()
			.FirstOrDefault(m =>
				m.Name == nameof(ParseResult.GetValue)
				&& m.IsGenericMethod
				&& m.GetParameters().Length == 1
				&& m.GetParameters()[0].ParameterType.IsGenericType
				&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Option<>)
			);
		if (getValueMethod == null)
		{
			throw new InvalidOperationException("Could not find generic GetValue<T>(Option<T>) method on ParseResult");
		}

		MethodInfo genericGetValue = getValueMethod.MakeGenericMethod(optionArgumentType);

		object? value = genericGetValue.Invoke(parseResult, new object[] { optionInstance });

		// Assign if value is present or property is nullable.
		if (value != null || IsNullableType(instanceProp.PropertyType))
		{
			if (value != null && value.GetType() != instanceProp.PropertyType)
			{
				throw new InvalidOperationException(
					$"Resolved value type '{value.GetType().Name}' does not match instance property '{instanceProp.Name}' of type '{instanceProp.PropertyType.Name}'.");
			}

			instanceProp.SetValue(this, value);
		}


		// Injects OptionResult into the OptionsModel's opt-in (nullable property)
		if (optionResultProp != null && optionResultProp.CanWrite)
		{
			optionResultProp.SetValue(this, parseResult.GetResult(optionInstance));
		}
	}

	/// <summary>
	///     Checks if a type is nullable.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if nullable, otherwise false.</returns>
	private static bool IsNullableType(Type type)
	{
		return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
	}

	/// <summary>
	///     Returns all defined static Option properties for this model type.
	/// </summary>
	/// <returns>A list of all options defined for this model.</returns>
	public List<Option> GetAllOptions()
	{
		return GetOrCreateOptionBinders().Keys.ToList();
	}

	public List<string> GetOptionPropertyValues()
	{
		List<string> result = new();
		Type type = GetType();

		List<PropertyInfo> optionProps = type
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType) && p.Name.EndsWith("Option"))
			.ToList();

		Dictionary<string, PropertyInfo> instanceProps = type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		string[] headers = { "Option", "Property", "Type", "Value" };

		int optionNameWidth =
			Math.Max(optionProps.Select(p => ((Option)p.GetValue(null)!).Name.Length).DefaultIfEmpty(0).Max(),
				headers[0].Length);
		int propertyNameWidth = Math.Max(instanceProps.Keys.Select(k => k.Length).DefaultIfEmpty(0).Max(),
			headers[1].Length);
		int propertyTypeWidth =
			Math.Max(instanceProps.Values.Select(p => p.PropertyType.Name.Length).DefaultIfEmpty(0).Max(),
				headers[2].Length);
		int valueWidth = headers[3].Length;

		string header = string.Format(
			"{0,-" + optionNameWidth + "}  {1,-" + propertyNameWidth + "}  {2,-" + propertyTypeWidth + "}  {3}",
			headers[0], headers[1], headers[2], headers[3]
		);
		result.Add(header);

		string underline = string.Format(
			"{0,-" + optionNameWidth + "}  {1,-" + propertyNameWidth + "}  {2,-" + propertyTypeWidth + "}  {3}",
			new string('-', headers[0].Length),
			new string('-', headers[1].Length),
			new string('-', headers[2].Length),
			new string('-', headers[3].Length)
		);
		result.Add(underline);

		foreach (PropertyInfo optionProp in optionProps)
		{
			string baseName = optionProp.Name.Substring(0, optionProp.Name.Length - "Option".Length);
			if (instanceProps.TryGetValue(baseName, out PropertyInfo? instanceProp))
			{
				Option? optionInstance = optionProp.GetValue(null) as Option;
				object? value = instanceProp.GetValue(this);

				string optionName = optionInstance?.Name ?? "(unknown)";
				string propertyName = instanceProp.Name;

				// Show List<string> instead of List`1
				string propertyType = instanceProp.PropertyType == typeof(List<string>)
					? "List<string>"
					: instanceProp.PropertyType.Name;

				// Render List<string> as comma-separated
				string valueStr;
				if (value is List<string> list)
				{
					valueStr = "[" + string.Join(", ", list.Select(s => $"\"{s}\"")) + "]";
				}
				else if (value is string str)
				{
					valueStr = $"\"{str}\"";
				}
				else if (value is FileSystemInfo info)
				{
					valueStr = $"\"{info}\"";
				}
				else
				{
					valueStr = value?.ToString() ?? "(null)";
				}

				result.Add(
					string.Format(
						"{0,-" + optionNameWidth + "}  {1,-" + propertyNameWidth + "}  {2,-" + propertyTypeWidth +
						"}  {3}",
						optionName, propertyName, propertyType, valueStr
					)
				);
			}
		}

		return result;
	}


	protected virtual void DoAddValidators()
	{
	}

	/// <summary>
	///     Performs validation and/or sets default values for this model after population.
	///     Override in derived classes to implement custom validation or defaulting logic.
	/// </summary>
	/// <param name="result">The parsed command line result.</param>
	protected virtual void DoValidateAndSetDefaults(ParseResult result)
	{
	}
}