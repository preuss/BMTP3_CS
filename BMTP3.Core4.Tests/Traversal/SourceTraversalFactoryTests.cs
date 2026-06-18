using BMTP3.Core4.Storage;
using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Traversal;

public class SourceTraversalFactoryTests
{
	[Fact]
	public void Create_FileSystemSource_ReturnsFileSystemTraversal()
	{
		FakeGatekeeper gatekeeper = new();
		SourceTraversalFactory factory = new(gatekeeper);
		IConnectedFileSystemSource source = new FakeConnectedFileSystemSource();

		ISourceTraversal traversal = factory.Create(source);

		Assert.IsType<FileSystemTraversal>(traversal);
	}

	[Fact]
	public void Create_NullSource_Throws()
	{
		FakeGatekeeper gatekeeper = new();
		SourceTraversalFactory factory = new(gatekeeper);

		Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
	}

	[Fact]
	public void Create_UnknownSourceType_Throws()
	{
		FakeGatekeeper gatekeeper = new();
		SourceTraversalFactory factory = new(gatekeeper);
		FakeUnknownSource source = new();

		Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(source));
	}

	[Fact]
	public void Constructor_NullGatekeeper_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new SourceTraversalFactory(null!));
	}

	private sealed class FakeGatekeeper : IMediaDeviceGatekeeper
	{
		public void Dispose() { }
		public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) => action(ct);
		public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);
		public Task<IDisposable> AcquireAsync(CancellationToken ct) => Task.FromResult<IDisposable>(new FakeLease());
		public Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct) => Task.FromResult<IDisposable>(new FakeLease());
		public IDisposable Acquire(CancellationToken ct) => new FakeLease();
		public IDisposable Acquire(TimeSpan timeout, CancellationToken ct) => new FakeLease();
		private sealed class FakeLease : IDisposable { public void Dispose() { } }
	}

	private sealed class FakeConnectedFileSystemSource : IConnectedFileSystemSource
	{
		public string Name => "FakeFS";
		public DriveInfo Drive => new("C:\\");
		public void Dispose() { }
	}

	private sealed class FakeUnknownSource : IConnectedSource
	{
		public string Name => "FakeUnknown";
		public void Dispose() { }
	}
}
