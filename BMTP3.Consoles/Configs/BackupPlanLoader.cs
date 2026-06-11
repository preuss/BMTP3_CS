using System.Text.Json;
using BMTP3.Core2.BackupNew.Api.Request;
using Tomlyn;

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
			TomlSerializerOptions options = new()
			{
				PropertyNamingPolicy = new SnakeCaseJsonNamingPolicy(),
			};

			BackupPlan? plan = TomlSerializer.Deserialize<BackupPlan>(content, options);
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


}