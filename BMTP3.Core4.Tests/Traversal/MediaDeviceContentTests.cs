using BMTP3.Core4.Models;
using BMTP3.Core4.Traversal;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Traversal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceContentTests
{
	private sealed class FakeGatekeeper : IMtpGatekeeper
	{
		public bool AcquireCalled { get; private set; }
		public IDisposable Lease { get; set; } = new TrackingDisposable();
		public Func<CancellationToken, Task<IDisposable>>? AcquireAsyncHandler { get; set; }
		public Func<CancellationToken, IDisposable>? AcquireHandler { get; set; }

		public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
			=> throw new NotImplementedException();

		public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
			=> throw new NotImplementedException();

		public async Task<IDisposable> AcquireAsync(CancellationToken ct)
		{
			AcquireCalled = true;
			if(AcquireAsyncHandler is not null)
				return await AcquireAsyncHandler(ct);
			await Task.CompletedTask;
			return Lease;
		}

		public Task<IDisposable> AcquireAsync(TimeSpan timeout, CancellationToken ct)
			=> AcquireAsync(ct);

		public IDisposable Acquire(CancellationToken ct)
		{
			AcquireCalled = true;
			if(AcquireHandler is not null)
				return AcquireHandler(ct);
			return Lease;
		}

		public IDisposable Acquire(TimeSpan timeout, CancellationToken ct)
		{
			AcquireCalled = true;
			if(AcquireHandler is not null)
				return AcquireHandler(ct);
			return Lease;
		}
	}

	private sealed class TrackingDisposable : IDisposable
	{
		public bool WasDisposed { get; private set; }
		public void Dispose() => WasDisposed = true;
	}

	[Fact]
	public void Constructor_NullMediaFileInfo_Throws()
	{
		var gatekeeper = new FakeGatekeeper();
		Assert.Throws<ArgumentNullException>(() => new MediaDeviceContent(null!, gatekeeper));
	}

	[Fact]
	public void Constructor_NullGatekeeper_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
		{
			var unused = new MediaDeviceContent(null!, null!);
		});
	}

	[Fact]
	public void Length_ReturnsMediaFileInfoLength()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var content = new MediaDeviceContent(fileInfo, new FakeGatekeeper());
		Assert.Equal(fileInfo.Length, content.Length);
	}

	[Fact]
	public void OpenRead_AcquiresLease()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var gatekeeper = new FakeGatekeeper();
		var content = new MediaDeviceContent(fileInfo, gatekeeper);

		using Stream stream = content.OpenRead();
		Assert.True(gatekeeper.AcquireCalled);
		Assert.IsType<GatekeptStream>(stream);
	}

	[Fact]
	public async Task OpenReadAsync_AcquiresLease()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var gatekeeper = new FakeGatekeeper();
		var content = new MediaDeviceContent(fileInfo, gatekeeper);

		using Stream stream = await content.OpenReadAsync(CancellationToken.None);
		Assert.True(gatekeeper.AcquireCalled);
		Assert.IsType<GatekeptStream>(stream);
	}

	[Fact]
	public void OpenRead_WhenAcquireFails_PropagatesException()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var gatekeeper = new FakeGatekeeper
		{
			AcquireHandler = ct => throw new InvalidOperationException("fail")
		};
		var content = new MediaDeviceContent(fileInfo, gatekeeper);

		Assert.Throws<InvalidOperationException>(() => content.OpenRead());
	}

	[Fact]
	public async Task OpenReadAsync_WhenAcquireFails_PropagatesException()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var gatekeeper = new FakeGatekeeper
		{
			AcquireAsyncHandler = ct => throw new InvalidOperationException("fail")
		};
		var content = new MediaDeviceContent(fileInfo, gatekeeper);

		await Assert.ThrowsAsync<InvalidOperationException>(() => content.OpenReadAsync(CancellationToken.None));
	}

	[Fact]
	public void OpenRead_WhenOpenReadFails_DisposesLease()
	{
		MediaFileInfo? fileInfo = GetFirstFile();
		if(fileInfo is null)
			return;

		var lease = new TrackingDisposable();
		var gatekeeper = new FakeGatekeeper
		{
			AcquireHandler = ct => lease
		};

		var content = new MediaDeviceContent(fileInfo, gatekeeper);
		using Stream stream = content.OpenRead();
		Assert.NotNull(stream);
	}

	private static MediaFileInfo? GetFirstFile()
	{
		MediaDevice? device = MediaDevice.GetDevices().FirstOrDefault();
		if(device is null)
			return null;

		const bool enableCache = false;
		device.Connect(MediaDeviceAccess.GenericRead, MediaDeviceShare.Read, enableCache);
		try
		{
			return device.GetDirectoryInfo(@"\").EnumerateFiles().FirstOrDefault();
		} finally
		{
			device.Disconnect();
		}
	}
}
