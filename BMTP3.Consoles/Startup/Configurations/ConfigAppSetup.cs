using BMTP3.Consoles.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace BMTP3.Consoles.Startup.Configurations;

public class ConfigAppSetup : IConfigSetup
{
	public void Configure(IConfigurationManager builder)
	{
		builder.SetBasePath(Directory.GetCurrentDirectory());
		builder.AddJsonFile("appsettings.json", false, false);

		string environment = GetCurrentEnvironmentValue(builder["Environment"]);
		builder.AddJsonFile($"appsettings.{environment}.json", true, false);

		builder.AddEnvironmentVariables();

		// Add Command Line arguments (Highest precedence)
		//builder.AddCommandLine(Program.Args);
	}

	private string GetCurrentEnvironmentValue(string? environmentValue, string fallbackEnvironmentValue = "Development")
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fallbackEnvironmentValue);

		// We read the environment name first (e.g., Development, Production)
		return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT").ToNullIfNullOrWhiteSpace()
		       ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToNullIfNullOrWhiteSpace()
		       ?? environmentValue.ToNullIfNullOrWhiteSpace()
		       ?? fallbackEnvironmentValue;
	}

	private string? GetFallbackConfigVariable(string key, IFileProvider fileProvider)
	{
		IConfigurationRoot tempConfig = new ConfigurationBuilder()
			.SetFileProvider(fileProvider)
			.AddJsonFile("appsettings.json", false)
			.Build();

		return tempConfig[key];
	}
}