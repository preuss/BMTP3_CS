using BMTP3.Core.CompareFiles;
using BMTP3.Core.Configs;
using BMTP3.Core.Handlers;
using BMTP3.Core.Handlers.Backup;
using BMTP3.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Runtime.Versioning;

namespace BMTP3.Core.Configuration {
	[SupportedOSPlatform("windows7.0")]
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

			ConfigureBackupServices(services);
			ConfigureFileComparisonServices(services);
			ConfigureMediaDeviceServices(services);
			ConfigureMiscellaneousServices(services);

			return services.BuildServiceProvider();
		}
		private void ConfigureBackupServices(IServiceCollection services) {
			services.AddSingleton<BackupSettingsReader>();
			services.AddSingleton<VerifyBackupHandler>();
			services.AddSingleton<BackupHelper>();
			services.AddSingleton<BackupHandler>();
			services.AddSingleton<StorageHandler>();
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
			services.AddSingleton<PrintHandler>();
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
				return new StartUp();
			} catch(Exception e) {
				Console.WriteLine($"An error occurred during initialization: {e.Message}");
				throw;
			}

		}
	}
}
