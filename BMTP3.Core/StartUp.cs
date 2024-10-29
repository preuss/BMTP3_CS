using BMTP3.Core.CompareFiles;
using BMTP3.Core.Configs;
using BMTP3.Core.Handlers;
using BMTP3.Core.Handlers.Backup;
using BMTP3.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Runtime.Versioning;

namespace BMTP3.Core {
	[SupportedOSPlatform("windows7.0")]
	internal class StartUp {
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly IConfiguration _configuration;

		public IServiceProvider ServiceProvider { get; private set; }

		public StartUp() : this(default, default) { }
		public StartUp(IConfiguration configuration) : this(configuration, default) { }

		public StartUp(IConfiguration? configuration, CancellationTokenSource? cancellationTokenSource) {
			_configuration = configuration ?? CreateConfiguration();
			_cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
			ServiceProvider = ConfigureServices(_configuration, _cancellationTokenSource, new ServiceCollection());
		}

		public IServiceProvider ConfigureServices(IConfiguration configuration, CancellationTokenSource cancellationTokenSource, IServiceCollection services) {
			var isTest = _configuration.GetValue<bool>("UseTestService");

			services.AddSingleton(_configuration);
			services.AddSingleton(_cancellationTokenSource);
			services.AddSingleton((service) => AnsiConsole.Console);
			services.AddSingleton<BackupSettingsReader>();
			services.AddSingleton<CancellationTokenGenerator>();

			int bufferSize = _configuration.GetValue("BufferSizeInKBForFileComparison", 8) * 1024;
			services.AddSingleton<FileComparer>((sp) => new ReadFileInChunksAndCompareSequenceEqual(bufferSize));

			services.AddSingleton<HashCalculator>();

			services.AddSingleton<PrintHandler>();

			services.AddSingleton<VerifyBackupHandler>();
			services.AddSingleton<BackupHelper>();
			services.AddSingleton<BackupHandler>();
			if(isTest) {
				services.AddSingleton<IMediaDeviceService, MediaDeviceServiceTest>();
			} else {
				services.AddSingleton<IMediaDeviceService, MediaDeviceServiceProd>();
			}

			services.AddSingleton<StorageHandler>();

			services.AddSingleton<BackupMaster>();

			return services.BuildServiceProvider();
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
	public class CancellationTokenGenerator : IDisposable {
		private readonly CancellationTokenSource _cancellationTokenSource;
		public CancellationTokenGenerator(CancellationTokenSource cancellationTokenSource) {
			_cancellationTokenSource = cancellationTokenSource;
		}
		public CancellationToken NewToken() => _cancellationTokenSource.Token;
		public CancellationTokenSource GetCancellationTokenSource() => _cancellationTokenSource;
		public void Dispose() {
			_cancellationTokenSource.Dispose();
		}
	}
}
