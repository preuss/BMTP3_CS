using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Traversal;
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
		// Register Core2 with a preConfigure callback so console app runs against the real scanner
		services.AddBMTP3Core2(s =>
		{
			// Use the high-level BackupScanner for filesystem runs in the console.
			// Consumers can still override this elsewhere.
			s.AddSingleton<IBackupScanner, BackupScanner>();
		});

		// Enable single-threaded debug mode for the engine when running the console app.
		// This uses the existing BackupEngineOptions.DebugSingleThreaded flag.
		services.Configure<BackupEngineOptions>(o => o.DebugSingleThreaded = true);

		// Override specific services if needed for the console app (e.g. Logging if not handled by generic host)
		// services.AddTransient<IMyLogic, MyLogic>();
	}
}