using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using BMTP3.Consoles.Logging;

namespace BMTP3.Consoles.Startup.Configurations;
public class LoggingServiceSetup : IServiceSetup
{
	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		// Register Spectre console for UI output
		services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

		services.AddLogging(loggingBuilder =>
		{
			loggingBuilder.ClearProviders();

			// Use only the Spectre ANSI console logger for colored terminal output
			loggingBuilder.AddProvider(new SpectreAnsiConsoleLoggerProvider(AnsiConsole.Console));

			loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

			loggingBuilder.SetMinimumLevel(LogLevel.Warning);

			loggingBuilder.AddFilter("System", LogLevel.Warning);
			loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
			loggingBuilder.AddFilter("BMTP3", LogLevel.Information);
		});
	}
}
