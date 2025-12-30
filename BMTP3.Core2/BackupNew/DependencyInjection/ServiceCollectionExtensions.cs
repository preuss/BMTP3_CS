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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace BMTP3.Core2.BackupNew.DependencyInjection;
/// <summary>
/// Extension helpers to register BMTP3.Core2 services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers BMTP3.Core2 services with sensible defaults.
	/// Callers may provide <paramref name="preConfigure"/> to register replacements before defaults are applied.
	/// Defaults are registered with TryAdd... so pre-registered services are preserved.
	/// </summary>
	/// <param name="services">The target service collection.</param>
	/// <param name="preConfigure">Optional callback to register or override services before defaults are added.</param>
	/// <returns>The original <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddBMTP3Core2(this IServiceCollection services, Action<IServiceCollection>? preConfigure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Allow caller to register overrides before adding defaults.
		preConfigure?.Invoke(services);

		// Engine defaults (use TryAdd so callers can provide replacements via preConfigure)
		services.TryAddSingleton<IMtpGatekeeper, MtpGatekeeper>();

		// High-level scanner default (safe no-op). Callers can override with BackupScanner.
		services.TryAddSingleton<IBackupScanner, NoopBackupScanner>();

		services.TryAddTransient<IStagingDownloader, StagingDownloader>();
		services.TryAddTransient<IHashGenerator, NoopHashGenerator>();

		services.TryAddTransient<IFileTransfer, LocalFileTransfer>();
		services.TryAddSingleton<IBackupRepository, FileBackupRepository>();

		// Strategies
		services.TryAddTransient<IPathGenerator, PathGenerator>();
		services.TryAddTransient<ICollisionResolver, CollisionResolver>();
		services.TryAddTransient<IMetadataReader, MetadataReader>();
		services.TryAddTransient<IItemHasher, ItemHasher>();
		services.TryAddTransient<ISidecarGenerator, JsonSidecarGenerator>();

		// Resilience
		services.TryAddTransient<IRetryPolicy>(sp =>
			new ExponentialBackoffRetryPolicy(3, 500, sp.GetService<ILogger<ExponentialBackoffRetryPolicy>>()));

		// Options
		services.Configure<BackupEngineOptions>(o =>
		{
			o.DegreeOfParallelism = 0; // 0 == Auto-detect
		});

		// Engine
		services.TryAddTransient<IJobValidator, JobValidator>();
		services.TryAddTransient<IBackupEngine, BackupEngine>();

		return services;
	}
}
