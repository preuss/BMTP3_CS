using System.Text;
using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace BMTP3.Consoles.Configs;

public static class BackupPlan4Loader
{
	public static BackupPlan4Config Load(FileInfo file)
	{
		if (file == null) throw new ArgumentNullException(nameof(file));
		if (!file.Exists) throw new FileNotFoundException($"Config file not found: {file.FullName}", file.FullName);

		string ext = file.Extension.ToLowerInvariant();
		string content = File.ReadAllText(file.FullName);

		if (ext == ".json")
		{
			JsonSerializerOptions options = new()
			{
				PropertyNameCaseInsensitive = true,
			};

			BackupPlan4Config? config = JsonSerializer.Deserialize<BackupPlan4Config>(content, options);
			if (config == null)
				throw new InvalidOperationException($"Failed to deserialize JSON config: {file.FullName}");

			return config;
		}

		if (ext == ".toml")
		{
			TomlModelOptions modelOptions = new()
			{
				ConvertPropertyName = PascalToKebab,
			};

			BackupPlan4Config? config;
			DiagnosticsBag? diagnostics;

			if (!Toml.TryToModel(content, out config, out diagnostics, null, modelOptions))
			{
				StringBuilder sb = new();
				sb.AppendLine($"Failed to parse TOML config: {file.FullName}");
				if (diagnostics != null)
				{
					foreach (DiagnosticMessage d in diagnostics)
					{
						sb.AppendLine(d.ToString());
					}
				}
				throw new InvalidOperationException(sb.ToString());
			}

			if (config == null)
				throw new InvalidOperationException($"Failed to parse config file: {file.FullName}");

			return config;
		}

		throw new NotSupportedException($"Unsupported config file extension '{ext}'. Supported: .toml, .json");
	}

	private static string PascalToKebab(string name)
	{
		if (string.IsNullOrEmpty(name)) return name;
		StringBuilder sb = new();
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (char.IsUpper(c))
			{
				if (i > 0) sb.Append('-');
				sb.Append(char.ToLowerInvariant(c));
			}
			else
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}
}
