using System.Text;
using System.Text.Json;

namespace BMTP3.Consoles.Configs;

/// <summary>
/// Helper methods for converting between naming conventions in TOML config files.
/// Originally based on BMTP3.Core.Configs.TomlNamingHelper2 (PascalCase/snake_case)
/// with kebab-case support added for Core4's BackupPlan4Config.
/// </summary>
public static class NamingPolicyHelper
{
	// ----------------------------------------------------------------
	// CamelCase <-> PascalCase
	// ----------------------------------------------------------------

	public static string PascalCaseToCamelCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		char firstLetter = char.ToLowerInvariant(name[0]);
		if (name.Length == 1) return firstLetter.ToString();
		return $"{firstLetter}{name.Substring(1)}";
	}

	public static string CamelCaseToPascalCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		char firstLetter = char.ToUpperInvariant(name[0]);
		if (name.Length == 1) return firstLetter.ToString();
		return $"{firstLetter}{name.Substring(1)}";
	}

	// ----------------------------------------------------------------
	// snake_case
	// ----------------------------------------------------------------

	public static string PascalCaseToSnakeCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		char prev = (char)0;

		foreach (char c in name)
		{
			if (char.IsUpper(c) && !char.IsUpper(prev) && prev != 0 && prev != '_')
			{
				result.Append('_');
			}
			result.Append(char.ToLowerInvariant(c));
			prev = c;
		}

		return result.ToString();
	}

	public static string SnakeCaseToPascalCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		bool capitalizeNext = true;

		foreach (char c in name)
		{
			if (c == '_')
			{
				capitalizeNext = true;
			}
			else
			{
				result.Append(capitalizeNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
				capitalizeNext = false;
			}
		}

		return result.ToString();
	}

	public static string SnakeCaseToCamelCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		bool capitalizeNext = false;

		foreach (char c in name)
		{
			if (c == '_')
			{
				capitalizeNext = true;
			}
			else
			{
				result.Append(capitalizeNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
				capitalizeNext = false;
			}
		}

		return result.ToString();
	}

	// ----------------------------------------------------------------
	// kebab-case
	// ----------------------------------------------------------------

	public static string PascalCaseToKebabCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		char prev = (char)0;

		foreach (char c in name)
		{
			if (char.IsUpper(c) && !char.IsUpper(prev) && prev != 0 && prev != '-')
			{
				result.Append('-');
			}
			result.Append(char.ToLowerInvariant(c));
			prev = c;
		}

		return result.ToString();
	}

	public static string KebabCaseToPascalCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		bool capitalizeNext = true;

		foreach (char c in name)
		{
			if (c is '-' or '_')
			{
				capitalizeNext = true;
			}
			else
			{
				result.Append(capitalizeNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
				capitalizeNext = false;
			}
		}

		return result.ToString();
	}

	public static string KebabCaseToCamelCase(string? name)
	{
		ArgumentNullException.ThrowIfNull(name);
		if (string.IsNullOrEmpty(name)) return name;

		StringBuilder result = new();
		bool capitalizeNext = false;

		foreach (char c in name)
		{
			if (c == '-')
			{
				capitalizeNext = true;
			}
			else
			{
				result.Append(capitalizeNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
				capitalizeNext = false;
			}
		}

		return result.ToString();
	}
}

/// <summary>
/// JsonNamingPolicy for snake_case (e.g. "include_patterns").
/// Converts PascalCase C# property names to snake_case TOML keys.
/// </summary>
public sealed class SnakeCaseJsonNamingPolicy : JsonNamingPolicy
{
	public override string ConvertName(string name) => NamingPolicyHelper.PascalCaseToSnakeCase(name);
}

/// <summary>
/// JsonNamingPolicy for kebab-case (e.g. "include-patterns").
/// Converts PascalCase C# property names to kebab-case TOML keys.
/// </summary>
public sealed class KebabCaseJsonNamingPolicy : JsonNamingPolicy
{
	public override string ConvertName(string name) => NamingPolicyHelper.PascalCaseToKebabCase(name);
}
