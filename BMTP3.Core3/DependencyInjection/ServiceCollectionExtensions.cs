using Microsoft.Extensions.DependencyInjection;
using BMTP3.Core3.Scanning;
using BMTP3.Core3.Transfer;
using BMTP3.Core3.Hashing;
using BMTP3.Core3.Metadata;
using BMTP3.Core3.Sidecar;

namespace BMTP3.Core3.DependencyInjection;

/// <summary>
/// Dependency injection setup for Core3 backup engine.
/// </summary>
public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddBMTP3Core3(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		// Register main entry point
		services.AddSingleton<IBackupEngine, BackupEngineSequential>();

		// Register scanners
		services.AddSingleton<IBackupScanner, FileSystemScanner>();

		// Register transfer
		services.AddSingleton<IFileTransfer, SimpleFileTransfer>();

		// Register hashing
		services.AddSingleton<IHashGenerator, StreamHashGenerator>();
		services.AddSingleton<IItemHasher, ItemHasher>();

		// Register metadata
		services.AddSingleton<IMetadataReader, FileMetadataReader>();

		// Register sidecar
		services.AddSingleton<ISidecarGenerator, SimpleSidecarGenerator>();

		return services;
	}
}
