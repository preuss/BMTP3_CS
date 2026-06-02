using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core4.Api;
using BMTP3.Core4.Engine;
using BMTP3.Core4.Engine.Compare;
using BMTP3.Core4.Engine.Compare.Algorithms;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

		// Traversal & scanner
		services.TryAddSingleton<ISourceTraversalFactory, SourceTraversalFactory>();
		services.TryAddSingleton<IBackupScanner, BackupScanner>();

		// Path & collision strategies
		services.TryAddSingleton<IMessageFormatter, BMTP3.Common.MessageFormatterParser.MessageFormatter>();
		services.TryAddSingleton<IFileFormatValuesFactory, FileFormatValuesFactory>();
		services.TryAddSingleton<ITargetPathResolver, TargetPathResolver>();
		services.TryAddSingleton<ICollisionResolver, CollisionResolver>();
		services.TryAddSingleton<IRenameCollisionResolver, RenameCollisionResolver>();

		// File compare
		services.TryAddSingleton<WholeFileSequenceEqualBinaryComparer>();
		services.TryAddSingleton<ChunkedSequenceEqualBinaryComparer>();
		services.TryAddSingleton<ChunkedVectorBinaryComparer>();
		services.TryAddSingleton<ChunkedEightByteBinaryComparer>();
		services.TryAddSingleton<ChunkedAvx2BinaryComparer>();
		services.TryAddSingleton<BinaryFileComparerSelector>();
		services.TryAddSingleton<IFileCompareService, FileCompareService>();

		// Engine
		services.TryAddSingleton<IBackupEngine>(sp =>
		{
			IBackupScanner scanner = sp.GetRequiredService<IBackupScanner>();
			ISourceTraversalFactory sourceTraversalFactory = sp.GetRequiredService<ISourceTraversalFactory>();
			IDownloadService downloadService = sp.GetRequiredService<IDownloadService>();
			IHashService hashService = sp.GetRequiredService<IHashService>();
			IEarliestTimestampResolutionService earliestTimestampService = sp.GetRequiredService<IEarliestTimestampResolutionService>();
			ISidecarService sidecarService = sp.GetRequiredService<ISidecarService>();
			IDiskSpaceValidator diskSpaceValidator = sp.GetRequiredService<IDiskSpaceValidator>();
			ILogger<BackupEngine> logger = sp.GetRequiredService<ILogger<BackupEngine>>();
			ITargetPathResolver targetPathResolver = sp.GetRequiredService<ITargetPathResolver>();
			ICollisionResolver collisionResolver = sp.GetRequiredService<ICollisionResolver>();
			return new BackupEngine(
				scanner,
				sourceTraversalFactory,
				downloadService,
				hashService,
				earliestTimestampService,
				sidecarService,
				diskSpaceValidator,
				logger,
				targetPathResolver,
				collisionResolver);
		});

		return services;
	}
}
