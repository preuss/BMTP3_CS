using BMTP3.Consoles.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Startup.Configurations;
public class ConfigAppSetup : IConfigSetup
{
	public void Configure(IConfigurationManager builder) {
		builder.SetBasePath(Directory.GetCurrentDirectory());
		builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
		
		String environment = GetCurrentEnvironmentValue(builder["Environment"]);
		builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
		
		builder.AddEnvironmentVariables();

		// Add Command Line arguments (Highest precedence)
		//builder.AddCommandLine(Program.Args);
	}

	private String GetCurrentEnvironmentValue(String? environmentValue, String fallbackEnvironmentValue = "Development") {
		ArgumentException.ThrowIfNullOrWhiteSpace(fallbackEnvironmentValue);

		// We read the environment name first (e.g., Development, Production)
		return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT").ToNullIfNullOrWhiteSpace()
			?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToNullIfNullOrWhiteSpace()
			?? environmentValue.ToNullIfNullOrWhiteSpace()
			?? fallbackEnvironmentValue;
	}
	private string? GetFallbackConfigVariable(string key, IFileProvider fileProvider) {
		var tempConfig = new ConfigurationBuilder()
			.SetFileProvider(fileProvider)
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		return tempConfig[key];
	}
}
