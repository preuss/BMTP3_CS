using System.Text;
using System.Text.Json;
using Tomlyn;

namespace BMTP3.Consoles.Configs;

public static class BackupPlan4Loader
{
	private static readonly string[] DefaultConfigCandidates = { "default.toml", "default.json", "default.json5" };

	public static FileInfo? FindDefaultConfig()
	{
		string cwd = Directory.GetCurrentDirectory();
		foreach(string name in DefaultConfigCandidates)
		{
			FileInfo fi = new(Path.Combine(cwd, name));
			if(fi.Exists) return fi;
		}
		return null;
	}

	public static BackupPlan4Config Load(FileInfo file)
	{
		if(file == null) throw new ArgumentNullException(nameof(file));
		if(!file.Exists) throw new FileNotFoundException($"Config file not found: {file.FullName}", file.FullName);

		string ext = file.Extension.ToLowerInvariant();
		string content = File.ReadAllText(file.FullName);

		if(ext == ".json" || ext == ".json5")
		{
			if(ext == ".json5")
				content = NormalizeJson5(content);

			JsonSerializerOptions options = new()
			{
				PropertyNameCaseInsensitive = true,
			};

			BackupPlan4Config? config = JsonSerializer.Deserialize<BackupPlan4Config>(content, options);
			if(config == null)
				throw new InvalidOperationException($"Failed to deserialize config: {file.FullName}");

			return config;
		}

		if(ext == ".toml")
		{
			TomlSerializerOptions options = new()
			{
				PropertyNamingPolicy = new KebabCaseJsonNamingPolicy(),
			};

			BackupPlan4Config? config = TomlSerializer.Deserialize<BackupPlan4Config>(content, options);

			if(config == null)
				throw new InvalidOperationException($"Failed to parse config file: {file.FullName}");

			return config;
		}

		throw new NotSupportedException($"Unsupported config file extension '{ext}'. Supported: .toml, .json, .json5");
	}

	internal static string NormalizeJson5(string raw)
	{
		if(string.IsNullOrEmpty(raw)) return raw;

		StringBuilder sb = new(raw.Length);
		bool inString = false;
		char stringDelim = '"';
		bool inLineComment = false;
		bool inBlockComment = false;

		for(int i = 0; i < raw.Length; i++)
		{
			char c = raw[i];

			// Inside a string — handle escapes and delimiter matching
			if(inString)
			{
				if(c == '\\' && i + 1 < raw.Length)
				{
					char next = raw[i + 1];
					// Unescape \' → ' when converting from single-quoted to double-quoted
					if(stringDelim == '\'' && next == '\'')
					{
						sb.Append('\'');
						i++;
					} else
					{
						sb.Append(c);
						sb.Append(next);
						i++;
					}
					continue;
				}

				if(c == stringDelim)
				{
					inString = false;
					sb.Append('"');
					continue;
				}

				// Escape embedded double quotes when converting from single-quoted
				if(stringDelim == '\'' && c == '"')
				{
					sb.Append('\\');
					sb.Append('"');
					continue;
				}

				sb.Append(c);
				continue;
			}

			// Line comment: //
			if(!inBlockComment && c == '/' && i + 1 < raw.Length && raw[i + 1] == '/')
			{
				inLineComment = true;
				i++;
				continue;
			}

			if(inLineComment)
			{
				if(c == '\n')
				{
					inLineComment = false;
					sb.Append(c);
				}
				continue;
			}

			// Block comment: /* ... */
			if(!inLineComment && c == '/' && i + 1 < raw.Length && raw[i + 1] == '*')
			{
				inBlockComment = true;
				i++;
				continue;
			}

			if(inBlockComment)
			{
				if(c == '*' && i + 1 < raw.Length && raw[i + 1] == '/')
				{
					inBlockComment = false;
					i++;
				}
				continue;
			}

			// String start — normalize to double quotes
			if(c == '"' || c == '\'')
			{
				inString = true;
				stringDelim = c;
				sb.Append('"');
				continue;
			}

			// Trailing comma — skip when followed by } or ]
			if(c == ',')
			{
				int j = i + 1;
				while(j < raw.Length && char.IsWhiteSpace(raw[j]))
					j++;
				if(j < raw.Length && (raw[j] == '}' || raw[j] == ']'))
					continue;
				sb.Append(c);
				continue;
			}

			// Unquoted key: identifier followed by :
			if(char.IsLetter(c) || c == '_' || c == '$')
			{
				int start = i;
				while(i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '_' || raw[i] == '$'))
					i++;

				string ident = raw[start..i];
				i--;

				int j = i + 1;
				while(j < raw.Length && char.IsWhiteSpace(raw[j]))
					j++;

				if(j < raw.Length && raw[j] == ':')
				{
					sb.Append('"');
					sb.Append(ident);
					sb.Append('"');
					continue;
				}

				sb.Append(ident);
				continue;
			}

			sb.Append(c);
		}

		return sb.ToString();
	}
}
