using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.Startup.Configurations;
public class ApplicationServiceSetup : IServiceSetup
{
	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSingleton<TimeProvider>(TimeProvider.System);

		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<IConsoleWriter, SystemConsoleWriter>();

		services.AddTransient<ConsoleApplication>();

		// Use the Core2 extension method to register all backup engine services
		services.AddBMTP3Core2();

		// Override specific services if needed for the console app (e.g. Logging if not handled by generic host)
		// services.AddTransient<IMyLogic, MyLogic>();
	}
}