using System.Text;
using System.Text.Json;
using System.Diagnostics;
using BMTP3.Core2.BackupNew.Api.Request;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace BMTP3.Consoles.Configs;

public static class BackupPlanLoader
{
	public static BackupPlan Load(FileInfo file)
	{
		if(file == null) throw new ArgumentNullException(nameof(file));
		if(!file.Exists) throw new FileNotFoundException($"Config file not found: {file.FullName}", file.FullName);

		string ext = file.Extension.ToLowerInvariant();
		string content = File.ReadAllText(file.FullName);
		if(ext == ".json")
		{
			JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			BackupPlan? plan = JsonSerializer.Deserialize<BackupPlan>(content, options);
			if(plan == null) throw new InvalidOperationException("Failed to deserialize config file to BackupPlan.");
			return plan;
		} else if(ext == ".toml")
		{
			TomlModelOptions modelOptions = new()
			{
				ConvertPropertyName = name => PascalToSnake(name),
				CreateInstance = MyCreateInstanceImpl
			};

			BackupPlan? plan;
			DiagnosticsBag? diagnostics;
			try
			{
				if(false == Toml.TryToModel(File.ReadAllText(file.FullName), out plan, out diagnostics, null, modelOptions))
				{
					StringBuilder sb = new();
					sb.AppendLine($"Failed to parse TOML config: {file.FullName}");
					if(diagnostics != null && diagnostics.Count > 0)
					{
						foreach(DiagnosticMessage d in diagnostics)
						{
							sb.AppendLine(d.ToString());
						}
					}
					throw new InvalidOperationException(sb.ToString());
				}
			} catch(FileNotFoundException)
			{
				throw;
			}

			if(plan == null)
			{
				throw new InvalidOperationException($"Failed to parse config file: {file.FullName}");
			}

			return plan;
		} else
		{
			throw new NotSupportedException($"Unsupported config file extension '{ext}'. Supported: .toml, .json");
		}
	}

	private static object MyCreateInstanceImpl(Type type, ObjectKind kind)
	{
		if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			Type inner = type.GetGenericArguments()[0];
			object? obj = Activator.CreateInstance(typeof(List<>).MakeGenericType(inner));
			if(obj != null)
			{
				return obj;
			} else
			{
				throw new InvalidOperationException("Null exception");
			}
		}

		if(type == typeof(object))
		{
			switch(kind)
			{
				case ObjectKind.Table:
				case ObjectKind.InlineTable:
					return new TomlTable(kind == ObjectKind.InlineTable);
				case ObjectKind.TableArray:
					return new TomlTableArray();
				default:
					Debug.Assert(kind == ObjectKind.Array);
					return new TomlArray();
			}
		}

		return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create an instance of type '{type.FullName}'");
	}

	private static string PascalToSnake(string name)
	{
		if(string.IsNullOrEmpty(name)) return name;
		StringBuilder sb = new();
		for(int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if(char.IsUpper(c))
			{
				if(i > 0) sb.Append('_');
				sb.Append(char.ToLowerInvariant(c));
			} else
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}
}