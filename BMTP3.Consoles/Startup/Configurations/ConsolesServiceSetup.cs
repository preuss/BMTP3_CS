using BMTP3.Consoles.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BMTP3.Core2.BackupNew.Api.UI;
using BMTP3.Consoles.UI;

namespace BMTP3.Consoles.Startup.Configurations;
public class ConsolesServiceSetup : IServiceSetup
{
	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<ConsolesPrinter>();
		// UI bindings for Core2 user-facing notifications and prompts
		services.AddSingleton<IUserNotifier, SpectreConsoleNotifier>();
		services.AddSingleton<IUserPrompter, SpectreConsolePrompter>();
	}
}
