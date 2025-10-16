using System;
using System.Collections.Generic;
using System.CommandLine;
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
	// Holds metadata for a validated parse delegate.
	private sealed record OptionPropertyInfo(
		string BaseName,
		Option OptionInstance,
		Type OptionValueType,
		PropertyInfo InstanceProperty,
		Delegate? ParseDelegate
	);
	private static string ExtractOptionBaseName(string optionPropertyName) =>
		optionPropertyName.Substring(0, optionPropertyName.Length - "Option".Length);



	public virtual void PopulateFromParseResult(ParseResult parseResult)
	{
		Type type = GetType();
		List<PropertyInfo> optionProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType))
			.Where(p => p.Name.EndsWith("Option"))
			.Where(p => p.PropertyType.IsConstructedGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Option<>))
			.ToList();

		Dictionary<string, PropertyInfo> instanceProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, p => p);

		//MethodInfo getValueMethod = GetParseResultMethodGetValueGeneric(parseResult);
		foreach(PropertyInfo optionProp in optionProps)
		{
			string baseName = optionProp.Name.Substring(0, optionProp.Name.Length - "Option".Length);
			if(!instanceProps.TryGetValue(baseName, out PropertyInfo? instanceProp))
			{
				throw new InvalidOperationException($"The corresponding instance property {baseName} does not exist for {optionProp.Name}");
			}

			BindOptionPropertyFromParseResult(parseResult, optionProp, instanceProp);
		}
	}

	private void BindOptionPropertyFromParseResult(ParseResult parseResult, PropertyInfo optionProp, PropertyInfo instanceProp)
	{
		// Get the generic argument type (T) from Option<T>
		Type optionType = optionProp.PropertyType;
		if(!(optionType.IsConstructedGenericType && optionType.GetGenericTypeDefinition() == typeof(Option<>)))
		{
			throw new InvalidOperationException($"{optionProp.Name} is not an Option<T>");
		}

		Type optionArgumentType = optionType.GetGenericArguments()[0];
		if(instanceProp.PropertyType != optionArgumentType)
		{
			throw new InvalidOperationException($"Type mismatch: {optionProp.Name} is Option<{optionArgumentType.Name}>, but {instanceProp.Name} is {instanceProp.PropertyType.Name}");
		}

		// Get the Option<T> instance from the static property
		if(optionProp.GetValue(null) is not Option optionInstance)
		{
			throw new InvalidOperationException($"Option instance for {optionProp.Name} is not an Option");
		}
		if(!optionType.IsInstanceOfType(optionInstance))
		{
			throw new InvalidOperationException($"Option instance for {optionProp.Name} is not of type Option<{optionArgumentType.Name}>. Actual type: {optionInstance.GetType()}");
		}

		// Find the generic GetValue<T>(Option<T>) method
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

		// Set the value on the instance property if not null and type matches
		if(value != null || IsNullableType(instanceProp.PropertyType))
		{
			if(value != null && value.GetType() != instanceProp.PropertyType)
			{
				throw new InvalidOperationException($"{instanceProp.Name} is type mismatch with Option<T>");
			}
			instanceProp.SetValue(this, value);
		}
	}
	private static bool IsNullableType(Type type)
	{
		return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
	}

	public List<Option> GetAllOptions()
	{
		var type = GetType();
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType))
			.Select(p => p.GetValue(null))
			.OfType<Option>()
			.ToList();
	}
}
