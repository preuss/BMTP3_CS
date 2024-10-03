using BMTP3_CS.CompareFiles;
using BMTP3_CS.Configs;
using BMTP3_CS.Handlers;
using BMTP3_CS.Handlers.Backup;
using BMTP3_CS.Services;
using MediaDevices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3_CS {
	[SupportedOSPlatform("windows7.0")]
	internal class StartUp {
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly IConfiguration _configuration;

		public IServiceProvider ServiceProvider { get; private set; }

		public StartUp() : this(default, default) { }
		public StartUp(IConfiguration configuration) : this(configuration, default) { }

		public StartUp(IConfiguration? configuration, CancellationTokenSource? cancellationTokenSource) {
			this._configuration = configuration ?? CreateConfiguration();
			this._cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
			this.ServiceProvider = this.ConfigureServices(this._configuration, this._cancellationTokenSource, new ServiceCollection());
		}

		public IServiceProvider ConfigureServices(IConfiguration configuration, CancellationTokenSource cancellationTokenSource, IServiceCollection services) {
			var isTest = _configuration.GetValue<bool>("UseTestService");

			services.AddSingleton(this._configuration);
			services.AddSingleton(this._cancellationTokenSource);
			services.AddSingleton((service) => AnsiConsole.Console);
			services.AddSingleton<BackupSettingsReader>();
			services.AddSingleton<CancellationTokenGenerator>();

			int bufferSize = _configuration.GetValue<int>("BufferSizeInKBForFileComparison", 8) * 1024;
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
	public class CancellationTokenGenerator {
		private readonly CancellationTokenSource _cancellationTokenSource;
		public CancellationTokenGenerator(CancellationTokenSource cancellationTokenSource) {
			this._cancellationTokenSource = cancellationTokenSource;
		}
		public CancellationToken Token => _cancellationTokenSource.Token;
	}
}
