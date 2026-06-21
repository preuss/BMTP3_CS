using BMTP3.Consoles.Services;
using BMTP3.Consoles.UI;
using BMTP3.Core2.BackupNew.Api.UI;
using BMTP3.Core4.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.Startup.Configurations;

public class ConsolesServiceSetup : IServiceSetup
{
	public void Configure(IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IFileSystemPathResolver, FileSystemPathResolver>();
		services.AddSingleton<ConsolesPrinter>();
		services.AddSingleton<ConsolesPrinter4>();
		services.AddSingleton<ConsolesPrinter2>();
		services.AddSingleton<ConsolesPrinter3>();
		// UI bindings for Core2 user-facing notifications and prompts
		services.AddSingleton<IUserNotifier, SpectreConsoleNotifier>();
		services.AddSingleton<IUserPrompter, SpectreConsolePrompter>();
	}
}