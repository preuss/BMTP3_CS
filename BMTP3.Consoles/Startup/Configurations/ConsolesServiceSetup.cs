using BMTP3.Consoles.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.Startup.Configurations;
public class ConsolesServiceSetup : IServiceSetup
{
	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<ConsolesPrinter>();
	}
}