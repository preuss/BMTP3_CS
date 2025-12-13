using Microsoft.Extensions.DependencyInjection;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Resilience;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using BMTP3.Core2.BackupNew.Engine;
using BMTP3.Core2.BackupNew.Api;

namespace BMTP3.Core2.BackupNew.DependencyInjection;
public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddBMTP3Core2(this IServiceCollection services)
	{
		// Register engine defaults. Host can override any of these registrations.
		services.AddSingleton<IDeviceScanner, NoopDeviceScanner>();
		services.AddTransient<IMediaToBackupItemConverter, MediaToBackupItemConverter>();
		services.AddTransient<IStagingDownloader, StagingDownloader>();
		services.AddTransient<IHashGenerator, NoopHashGenerator>();
		services.AddTransient<IFileTransfer, LocalFileTransfer>();
		services.AddSingleton<IBackupRepository, FileBackupRepository>();

		// Strategy defaults
		services.AddTransient<IPathGenerator, PathGenerator>();
		services.AddTransient<ICollisionResolver, CollisionResolver>();
		services.AddTransient<IMetadataExtractor, MetadataExtractor>();

		// Resilience: Retry policy
		services.AddTransient<IRetryPolicy>(sp =>
		{
			var logger = sp.GetService<ILogger<ExponentialBackoffRetryPolicy>>();
			return new ExponentialBackoffRetryPolicy(maxAttempts: 3, baseBackoffMs: 200, logger);
		});

		// Orchestration: Worker pool processor
		//services.AddTransient<BackupItemProcessor>();

		// Register configurable options with sane defaults
		services.Configure<BmtpOptions>(o =>
		{
			o.DegreeOfParallelism = 0; // Auto-detect
		});

		// Engine itself
		services.AddTransient<IBackupEngine, BackupEngine>();

		return services;
	}
}