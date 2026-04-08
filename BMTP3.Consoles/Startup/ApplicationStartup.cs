using BMTP3.Consoles.Startup.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.Startup;

public class ApplicationStartup
{
	public static IServiceProvider InitializeServiceProvider(string[] args)
	{
		//IHostBuilder builder = Host.CreateDefaultBuilder(args);
		//IConfigurationBuilder builder = new ConfigurationBuilder();

		// Build configuration
		IConfigurationManager configBuilder = new ConfigurationManager();

		List<IConfigSetup> configurators =
		[
			new ConfigAppSetup()
		];

		foreach (IConfigSetup configurator in configurators)
		{
			configurator.Configure(configBuilder);
		}

		IConfigurationRoot configuration = configBuilder.Build();

		// Build DI-container/host
		ServiceCollection services = new();
		List<IServiceSetup> serviceSetupList =
		[
			new LoggingServiceSetup(),
			new ApplicationServiceSetup()
		];
		foreach (IServiceSetup serviceSetup in serviceSetupList)
		{
			serviceSetup.Configure(services, configuration);
		}

		return services.BuildServiceProvider();
	}
}