using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;

namespace BMTP3.Consoles.ConsoleCommands;
public abstract class BaseOptionsModel
{
    public virtual void PopulateFromParseResult(ParseResult parseResult)
    {
        var type = GetType();
        var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static);

        // Validate all Parse{Name}Option delegates
        foreach (var parseProp in staticProps.Where(p => p.Name.StartsWith("Parse") && p.Name.EndsWith("Option")))
        {
            var baseName = parseProp.Name.Substring("Parse".Length, parseProp.Name.Length - "Parse".Length - "Option".Length);

            var instanceProp = type.GetProperty(baseName, BindingFlags.Public | BindingFlags.Instance);
            if (instanceProp == null)
            {
                throw new InvalidOperationException(
                    $"Parse delegate '{parseProp.Name}' exists, but there is no instance property '{baseName}' on type '{type.FullName}'. " +
                    $"Add a property '{baseName}' or remove '{parseProp.Name}'.");
            }

            if (!instanceProp.CanWrite)
            {
                throw new InvalidOperationException(
                    $"Property '{baseName}' on '{type.FullName}' is not writable, but '{parseProp.Name}' is declared.");
            }

            if (parseProp.GetValue(null) is not Delegate parserFunc)
            {
                throw new InvalidOperationException(
                    $"Parse delegate property '{parseProp.Name}' on '{type.FullName}' is null or not a delegate.");
            }

            var parserReturnType = parserFunc.GetType().GetMethod("Invoke")?.ReturnType;
            var propertyType = instanceProp.PropertyType;
            if (parserReturnType is not null && !propertyType.IsAssignableFrom(parserReturnType))
            {
                throw new InvalidOperationException(
                    $"Type mismatch: '{parseProp.Name}' returns {parserReturnType.FullName}, " +
                    $"but property '{baseName}' is of type {propertyType.FullName} in {type.FullName}.");
            }
        }

        // Track Parse{Name}Option assignments
        HashSet<string> assigned = new(StringComparer.Ordinal);

        foreach (var parseProp in staticProps.Where(p => p.Name.StartsWith("Parse") && p.Name.EndsWith("Option")))
        {
            var baseName = parseProp.Name.Substring("Parse".Length, parseProp.Name.Length - "Parse".Length - "Option".Length);

            var instanceProp = type.GetProperty(baseName, BindingFlags.Public | BindingFlags.Instance);
            if (instanceProp == null || !instanceProp.CanWrite)
            {
                continue;
            }

            if (parseProp.GetValue(null) is not Delegate parserFunc)
            {
                continue;
            }

            object? value = parserFunc.DynamicInvoke(parseResult);
            if (value != null)
            {
                instanceProp.SetValue(this, value);
                assigned.Add(baseName);
            }
        }

        foreach (var optionProp in staticProps.Where(p => typeof(Option).IsAssignableFrom(p.PropertyType)))
        {
            // E.g. VerboseOption -> Verbose
            string baseName = optionProp.Name.EndsWith("Option", StringComparison.Ordinal)
                ? optionProp.Name[..^"Option".Length]
                : optionProp.Name;

            if (assigned.Contains(baseName))
            {
                // Already delegated to Parse{Name}Option
                continue;
            }

            var instanceProp = type.GetProperty(baseName, BindingFlags.Public | BindingFlags.Instance);
            if (instanceProp == null || !instanceProp.CanWrite)
            {
                continue;
            }

            if (optionProp.GetValue(null) is not Option opt)
            {
                continue;
            }

            object? value = GetOptionValue(parseResult, opt, instanceProp.PropertyType);
            if (value != null)
            {
                instanceProp.SetValue(this, value);
            }
        }
    }

    private static object? GetOptionValue(ParseResult parseResult, Option opt, Type targetType)
    {
        var optionType = typeof(Option<>).MakeGenericType(targetType);
        if (!optionType.IsInstanceOfType(opt))
        {
            throw new InvalidOperationException($"Option instance is not of type Option<{targetType.Name}>. Actual type: {opt.GetType()}");
        }
        var method = typeof(ParseResult)
            .GetMethods()
            .Where(m =>
                m.Name == nameof(ParseResult.GetValue) &&
                m.IsGenericMethod &&
                m.GetParameters().Length == 1
            )
            .FirstOrDefault(m =>
                m.GetParameters()[0].ParameterType.IsGenericType &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Option<>)
            );

        if (method != null)
        {
            var generic = method.MakeGenericMethod(targetType);
            object castedOpt = Convert.ChangeType(opt, optionType);
            return generic.Invoke(parseResult, new object[] { castedOpt });
        }
        return null;
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

    public IEnumerable<Delegate> GetAllParseOptions()
    {
        var type = GetType();
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p =>
                p.Name.StartsWith("Parse") &&
                p.Name.EndsWith("Option") &&
                typeof(Delegate).IsAssignableFrom(p.PropertyType)
            )
            .Select(p => p.GetValue(null))
            .OfType<Delegate>();
    }
}
