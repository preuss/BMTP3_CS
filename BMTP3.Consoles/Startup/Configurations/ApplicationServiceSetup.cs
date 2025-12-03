using BMTP3.Consoles.Services;
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

		// services.AddTransient<IMyLogic, MyLogic>();
	}
}