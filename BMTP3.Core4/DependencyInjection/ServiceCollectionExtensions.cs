using BMTP3.Core4.Api;
using BMTP3.Core4.Engine;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Runner;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BMTP3.Core4.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddBMTP3Core4(this IServiceCollection services)
	{
		// Hashing
		services.TryAddSingleton<IHashGenerator, StreamHashGenerator>();
		services.TryAddSingleton<IHashService, HashService>();

		// Pre-flight validation
		services.TryAddSingleton<IDiskSpaceValidator, DiskSpaceValidator>();

		// Engine services
		services.TryAddSingleton<IDownloadService, DownloadService>();
		services.TryAddSingleton<IEarliestTimestampResolutionService, EarliestTimestampResolutionService>();
		services.TryAddSingleton<ISidecarService, SidecarService>();
		services.TryAddSingleton<IBackupRunnerFactory, BackupRunnerFactory>();
		services.TryAddTransient<IBackupRunner, BackupRunner>();

		// Traversal & scanner
		services.TryAddSingleton<ISourceTraversalFactory, SourceTraversalFactory>();
		services.TryAddSingleton<IBackupScanner, BackupScanner>();

		// Engine
		services.TryAddSingleton<IBackupEngine, BackupEngine>();

		return services;
	}
}
