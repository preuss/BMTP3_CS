using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Repositories;
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
using MediaDevices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMTP3.Core2.BackupNew.DependencyInjection;

/// <summary>
///     Extension helpers to register BMTP3.Core2 services into an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers BMTP3.Core2 services with sensible defaults.
	///     Callers may provide <paramref name="preConfigure" /> to register replacements before defaults are applied.
	///     Defaults are registered with TryAdd... so pre-registered services are preserved.
	/// </summary>
	/// <param name="services">The target service collection.</param>
	/// <param name="preConfigure">Optional callback to register or override services before defaults are added.</param>
	/// <returns>The original <paramref name="services" /> for chaining.</returns>
	public static IServiceCollection AddBMTP3Core2(this IServiceCollection services,
		Action<IServiceCollection>? preConfigure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Allow caller to register overrides before adding defaults.
		preConfigure?.Invoke(services);

		// Engine defaults (use TryAdd so callers can provide replacements via preConfigure)
		services.TryAddSingleton<IMediaDeviceScannerFactory, MediaDeviceScannerFactory>();

		// MTP Gatekeeper with configurable timeout from BackupEngineOptions
		services.TryAddSingleton<IMtpGatekeeper>(sp =>
		{
			IOptions<BackupEngineOptions>? options = sp.GetService<IOptions<BackupEngineOptions>>();
			int timeout = options?.Value.MtpOperationTimeoutMs ?? 60000;
			return new MtpGatekeeper(timeout);
		});

		// Traversal scanner defaults: FileSystemScanner for local disk.
		// MediaDeviceScanner cannot be registered here because it requires a MediaDevice instance
		// at construction time (device-specific dependency). Callers that need MTP scanning must
		// register ITraversalScanner<MediaFileInfo> themselves, or BackupScanner will fall back
		// to throwing NotSupportedException for MediaDevice source type.
		services.TryAddSingleton<ITraversalScanner<FileInfo>, FileSystemScanner>();
		// NoopMediaDeviceScanner is the default fallback. Callers that need real MTP scanning must
		// register a MediaDeviceScanner bound to a specific device via preConfigure before calling AddBMTP3Core2.
		services.TryAddSingleton<ITraversalScanner<MediaFileInfo>, NoopMediaDeviceScanner>();

		// High-level scanner default. Uses the real BackupScanner which supports both FileSystem and MTP.
		services.TryAddSingleton<IBackupScanner, BackupScanner>();

		services.TryAddTransient<IStagingDownloader, StagingDownloader>();
		// Default to a real hash generator for integration runs. The NoopHashGenerator
		// remains in the codebase as a test/placeholder, but the default should
		// compute real hashes so collision resolution and verification work.
		services.TryAddTransient<IHashGenerator, StreamHashGenerator>();

		services.TryAddTransient<IFileTransfer, LocalFileTransfer>();
		services.TryAddSingleton<IBackupRepository, FileBackupRepository>();

		// Strategies
		services.TryAddTransient<IPathGenerator, PathGenerator>();
		services.TryAddTransient<ICollisionResolver, CollisionResolver>();
		services.TryAddTransient<IMetadataReader, MetadataReader>();
		services.TryAddTransient<ITimestampWaterfall, DefaultTimestampWaterfall>();
		// Destination inspector (provides cached destination snapshot & hashing helpers)
		services.TryAddSingleton<IDestinationInspector, DestinationInspector>();
		services.TryAddTransient<IItemHasher, ItemHasher>();
		services.TryAddTransient<ISidecarGenerator, JsonSidecarGenerator>();
		services.TryAddTransient<JsonSidecarGenerator>();
		services.TryAddTransient<IniSidecarGenerator>();
		services.TryAddSingleton<ISidecarGeneratorFactory>(sp =>
		{
			Dictionary<SidecarFormat, ISidecarGenerator> generators = new()
			{
				[SidecarFormat.Json] = sp.GetRequiredService<JsonSidecarGenerator>(),
				[SidecarFormat.Ini] = sp.GetRequiredService<IniSidecarGenerator>()
			};
			return new SidecarGeneratorFactory(generators);
		});

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