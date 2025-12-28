using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Engine;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Resilience;
using BMTP3.Core2.BackupNew.Engine.Staging;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Infrastructure.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMTP3.Core2.BackupNew.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddBMTP3Core2(this IServiceCollection services)
	{
		// Engine defaults
		services.AddSingleton<IDeviceScanner, NoopDeviceScanner>();
        services.AddSingleton<IMtpGatekeeper, MtpGatekeeper>();
		services.AddTransient<IMediaToBackupItemConverter, MediaToBackupItemConverter>();
		services.AddTransient<IStagingDownloader, StagingDownloader>();
		// IHashGenerator is now consumed by IItemHasher, but can still be registered for direct use if needed
		services.AddTransient<IHashGenerator, NoopHashGenerator>(); 

		services.AddTransient<IFileTransfer, LocalFileTransfer>();
		services.AddSingleton<IBackupRepository, FileBackupRepository>();

		// Strategies
		services.AddTransient<IPathGenerator, PathGenerator>();
		services.AddTransient<ICollisionResolver, CollisionResolver>();
		services.AddTransient<IMetadataReader, MetadataReader>(); // Changed from IMetadataExtractor
		services.AddTransient<IItemHasher, ItemHasher>(); // Added IItemHasher
		services.AddTransient<ISidecarGenerator, JsonSidecarGenerator>();

		// Resilience
		services.AddTransient<IRetryPolicy>(sp => 
			new ExponentialBackoffRetryPolicy(3, 500, sp.GetService<ILogger<ExponentialBackoffRetryPolicy>>()));

		// Options
		services.Configure<BackupEngineOptions>(o =>
		{
			o.DegreeOfParallelism = 0; // 0 == Auto-detect
		});

		// Engine
        services.AddTransient<IJobValidator, JobValidator>();
		services.AddTransient<IBackupEngine, BackupEngine>();

		return services;
	}
}
