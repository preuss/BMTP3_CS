using BMTP3.Core4.Api;
using BMTP3.Core4.DependencyInjection;
using BMTP3.Core4.Engine.Compare;
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

namespace BMTP3.Core4.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
	private static IServiceProvider CreateProvider()
	{
		ServiceCollection services = new();
		services.AddLogging();
		services.AddBMTP3Core4();
		return services.BuildServiceProvider();
	}

	[Fact]
	public void AddBMTP3Core4_EngineCanBeResolved()
	{
		IServiceProvider provider = CreateProvider();

		IBackupEngine? engine = provider.GetService<IBackupEngine>();

		Assert.NotNull(engine);
	}

	[Fact]
	public void AddBMTP3Core4_ResolvesAllCoreServices()
	{
		IServiceProvider provider = CreateProvider();

		Assert.NotNull(provider.GetService<IHashGenerator>());
		Assert.NotNull(provider.GetService<IHashService>());
		Assert.NotNull(provider.GetService<IDiskSpaceValidator>());
		Assert.NotNull(provider.GetService<IDownloadService>());
		Assert.NotNull(provider.GetService<IEarliestTimestampResolutionService>());
		Assert.NotNull(provider.GetService<ISidecarService>());
		Assert.NotNull(provider.GetService<ISourceTraversalFactory>());
		Assert.NotNull(provider.GetService<IBackupScanner>());
		Assert.NotNull(provider.GetService<ITargetPathResolver>());
		Assert.NotNull(provider.GetService<ICollisionResolver>());
		Assert.NotNull(provider.GetService<IRenameCollisionResolver>());
		Assert.NotNull(provider.GetService<IFileCompareService>());
		Assert.NotNull(provider.GetService<IBackupEngine>());
	}

	[Fact]
	public void AddBMTP3Core4_Singletons_AreSameInstance()
	{
		IServiceProvider provider = CreateProvider();

		IHashService a = provider.GetRequiredService<IHashService>();
		IHashService b = provider.GetRequiredService<IHashService>();

		Assert.Same(a, b);
	}
}
