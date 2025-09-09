using BMTP3.Core.CompareFiles;
using BMTP3.Core.Configs;
using BMTP3.Core.Handlers;
using BMTP3.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Runtime.Versioning;
using ZLogger;

namespace BMTP3.Core.Configuration {
	[SupportedOSPlatform("windows10.0")]
	internal class StartUp {
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly IConfiguration _configuration;

		public IServiceProvider ServiceProvider { get; private set; }

		public StartUp() : this(default, default) { }

		public StartUp(IConfiguration? configuration, CancellationTokenSource? cancellationTokenSource) {
			_configuration = configuration ?? CreateConfiguration();
			_cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
			ServiceProvider = ConfigureServices(_configuration, _cancellationTokenSource, new ServiceCollection());
		}

		public IServiceProvider ConfigureServices(IConfiguration configuration, CancellationTokenSource cancellationTokenSource, IServiceCollection services) {
			services.AddSingleton(_configuration);
			services.AddSingleton(_cancellationTokenSource);
			services.AddSingleton((service) => AnsiConsole.Console);

			// Add logging
			services.AddLogging(builder => {
				builder.AddZLoggerConsole(); // Add ZLogger
				builder.AddConsole(); // Add standard console-logging as fallback
			});

			ConfigureBackupServices(services);
			ConfigureFileComparisonServices(services);
			ConfigureMediaDeviceServices(services);
			ConfigureMiscellaneousServices(services);

			return services.BuildServiceProvider();
		}
		private void ConfigureBackupServices(IServiceCollection services) {
			services.AddSingleton<BackupSettingsReader>();
			services.AddSingleton<VerifyBackupHandler>();
			services.AddTransient<BackupHelper>();
			services.AddSingleton<IBackupHandler, BackupHandler>();
			services.AddSingleton<IStorageHandler, MediaDeviceHandler>();
			services.AddSingleton<IDriveHandler, DriveHandler>();
			services.AddSingleton<BackupMaster>();
		}
		private void ConfigureFileComparisonServices(IServiceCollection services) {
			// Best performance with 512 * 1024
			int bufferSize = _configuration.GetValue("BufferSizeInKBForFileComparison", 8) * 1024;
			services.AddSingleton<FileComparer>((sp) => new ReadFileInChunksAndCompareSequenceEqual(bufferSize));
			services.AddSingleton<HashCalculator>();
		}
		private void ConfigureMediaDeviceServices(IServiceCollection services) {
			var isTest = _configuration.GetValue<bool>("UseTestService");
			if(isTest) {
				services.AddSingleton<IMediaDeviceService, MediaDeviceServiceTest>();
			} else {
				services.AddSingleton<IMediaDeviceService, MediaDeviceServiceProd>();
			}
		}
		private void ConfigureMiscellaneousServices(IServiceCollection services) {
			services.AddSingleton<IPrintHandler, PrintHandler>();
			services.AddSingleton<BackupExceptionHandlerService>();
		}
		private IConfiguration CreateConfiguration() {
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			var tempConfig = builder.Build();

			var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? tempConfig["Environment"];

			builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

			return builder.Build();
		}

		public static StartUp CreateAndInitialize() {
			try {
				var startup = new StartUp();
				var logger = startup.ServiceProvider.GetService<ILogger<StartUp>>();
				logger?.LogInformation("StartUp initialized successfully.");
				return startup;
			} catch(Exception e) {
				Console.WriteLine($"An error occurred during initialization: {e.Message}");
				throw;
			}

		}
	}
}
