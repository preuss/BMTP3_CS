using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace BMTP3.Consoles.ConsoleCommands;
/// <summary>
/// Base class for command line options models. 
/// Automatically binds static Option{T} properties to instance properties using naming convention.
/// </summary>
/// <remarks>
/// Convention: For each static property named {Name}Option of type Option{T}, 
/// there must be an instance property named {Name} of type T.
/// </remarks>
public abstract class BaseOptionsModel
{
	private static readonly Dictionary<Type, Dictionary<Option, Action<ParseResult>>> _optionBindersCache = new();

	protected Dictionary<Option, Action<ParseResult>> GetOrCreateOptionBinders()
	{
		// TODO: Add thread safe caching
		var type = GetType();
		if (!_optionBindersCache.TryGetValue(type, out var binders))
		{
			binders = DoDefineOptions();
			_optionBindersCache[type] = binders;
		}
		return binders;
	}

	public static void ValidateDuplicateNameAndAlias(IEnumerable<BaseOptionsModel> models)
	{
		// Collect all option names and aliases, with references to their Option and model Type
		var allNames = models
            .SelectMany(optionsModel => optionsModel.GetAllOptions().Select(option =>
                new {
	                Names = (new[] { option.Name }).Concat(option.Aliases),
					Option = option,
                    ModelType = optionsModel.GetType(),
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
            throw new InvalidOperationException("Duplicate option names or aliases detected:" + Environment.NewLine + msg);
        }
	}
	protected virtual Dictionary<Option, Action<ParseResult>> DoDefineOptions()
	{
		Dictionary<Option, Action<ParseResult>> dict = new();

		Type type = GetType();
		List<PropertyInfo> optionProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType))
			.Where(p => p.Name.EndsWith("Option", StringComparison.Ordinal))
			//.Where(p => p.PropertyType.IsConstructedGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Option<>))
			.Where(p => IsGenericTypeAssignableFrom(p.PropertyType, typeof(Option<>)))
			.ToList();

		Dictionary<string, PropertyInfo> instanceProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		//MethodInfo getValueMethod = GetParseResultMethodGetValueGeneric(parseResult);
		foreach(PropertyInfo optionProp in optionProps) {
			string baseName = ExtractOptionBaseName(optionProp.Name);
			if(!instanceProps.TryGetValue(baseName, out PropertyInfo? instanceProp)) {
				throw new InvalidOperationException($"The corresponding instance property {baseName} does not exist for {optionProp.Name}");
			}

			Option? optionInstance = (Option?)optionProp.GetValue(null);
			ArgumentNullException.ThrowIfNull(optionInstance);
			dict.Add(optionInstance, parseResult => BindOptionPropertyFromParseResult(parseResult, optionProp, instanceProp));
		}

		return dict;
	}
	private static string ExtractOptionBaseName(string optionPropertyName) =>
		optionPropertyName.Substring(0, optionPropertyName.Length - "Option".Length);


	/// <summary>
	/// Populates instance properties from a <see cref="ParseResult"/> using the static Option{T} properties.
	/// </summary>
	/// <param name="parseResult">The parse result produced by System.CommandLine.</param>

	public virtual void PopulateFromParseResult(ParseResult parseResult)
	{
		Dictionary<Option, Action<ParseResult>> optionBinders = GetOrCreateOptionBinders();
		foreach (var binder in optionBinders.Values)
		{
			binder(parseResult);
		}
	}
	private static Type? GetGenericType(Type? candidate, Type genericTypeDefinition)
	{
		// traverse inheritance hierarchy check if any base is a genericTypeDefinition
		while(candidate != null && candidate != typeof(object))
		{
			if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericTypeDefinition)
			{
				return candidate.GetGenericArguments()[0];
			}

			candidate = candidate.BaseType!;
		}
		return null;
	}
	private static bool IsGenericTypeAssignableFrom(Type? candidate, Type genericTypeDefinition)
	{
		// traverse inheritance hierarchy check if any base is a genericTypeDefinition
		return GetGenericType(candidate, genericTypeDefinition) != null;
	}
	private void BindOptionPropertyFromParseResult(ParseResult parseResult, PropertyInfo optionProp, PropertyInfo instanceProp)
	{
		// Get the generic argument type (T) from Option<T>
		Type optionType = optionProp.PropertyType;
		Type? optionArgumentType = GetGenericType(optionType, typeof(Option<>));
		if(optionArgumentType == null)
		{
			throw new InvalidOperationException($"{optionProp.Name} is not an Option<T>");
		}

		if(instanceProp.PropertyType != optionArgumentType)
		{
			throw new InvalidOperationException($"Type mismatch: {optionProp.Name} is Option<{optionArgumentType.Name}>, but {instanceProp.Name} is {instanceProp.PropertyType.Name}");
		}

		if(optionProp.GetValue(null) is not Option optionInstance)
		{
			throw new InvalidOperationException($"Option instance for {optionProp.Name} is not an Option");
		}
		if(!optionType.IsInstanceOfType(optionInstance))
		{
			throw new InvalidOperationException($"Option instance for {optionProp.Name} is not of type Option<{optionArgumentType.Name}>. Actual type: {optionInstance.GetType()}");
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
		if(getValueMethod == null)
		{
			throw new InvalidOperationException("Could not find generic GetValue<T>(Option<T>) method on ParseResult");
		}

		MethodInfo genericGetValue = getValueMethod.MakeGenericMethod(optionArgumentType);

		object? value = genericGetValue.Invoke(parseResult, new object[] { optionInstance });

		// Assign if value is present or property is nullable.
		if(value != null || IsNullableType(instanceProp.PropertyType))
		{
			if(value != null && value.GetType() != instanceProp.PropertyType)
			{
				throw new InvalidOperationException($"Resolved value type '{value.GetType().Name}' does not match instance property '{instanceProp.Name}' of type '{instanceProp.PropertyType.Name}'.");

			}
			instanceProp.SetValue(this, value);
		}
	}
	private static bool IsNullableType(Type type)
	{
		return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
	}
	/// <summary>
	/// Returns all defined static Option properties for this model type.
	/// </summary>
	public List<Option> GetAllOptions()
	{
		return GetOrCreateOptionBinders().Keys.ToList();
	}
}
