using BMTP3.Core4.Models;

namespace BMTP3.Core4.Tests.Traversal;

public class GatekeptStreamTests
{
	[Fact]
	public void Constructor_NullInner_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new GatekeptStream(null!, new TrackingDisposable()));
	}

	[Fact]
	public void Constructor_NullLease_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new GatekeptStream(new MemoryStream(), null!));
	}

	[Fact]
	public void CanWrite_IsFalse()
	{
		using var sut = new GatekeptStream(new MemoryStream(), new TrackingDisposable());
		Assert.False(sut.CanWrite);
	}

	[Fact]
	public void Write_ThrowsNotSupported()
	{
		using var sut = new GatekeptStream(new MemoryStream(), new TrackingDisposable());
		Assert.Throws<NotSupportedException>(() => sut.Write([], 0, 0));
	}

	[Fact]
	public void SetLength_ThrowsNotSupported()
	{
		using var sut = new GatekeptStream(new MemoryStream(), new TrackingDisposable());
		Assert.Throws<NotSupportedException>(() => sut.SetLength(0));
	}

	[Fact]
	public void Read_DelegatesToInner()
	{
		byte[] expected = [1, 2, 3];
		using var inner = new MemoryStream(expected);
		using var sut = new GatekeptStream(inner, new TrackingDisposable());

		byte[] buffer = new byte[3];
		int read = sut.Read(buffer, 0, 3);

		Assert.Equal(3, read);
		Assert.Equal(expected, buffer);
	}

	[Fact]
	public async Task ReadAsync_DelegatesToInner()
	{
		byte[] expected = [4, 5, 6];
		using var inner = new MemoryStream(expected);
		using var sut = new GatekeptStream(inner, new TrackingDisposable());

		byte[] buffer = new byte[3];
		int read = await sut.ReadAsync(buffer, 0, 3);

		Assert.Equal(3, read);
		Assert.Equal(expected, buffer);
	}

	[Fact]
	public void Seek_DelegatesToInner()
	{
		byte[] data = [1, 2, 3, 4, 5];
		using var inner = new MemoryStream(data);
		using var sut = new GatekeptStream(inner, new TrackingDisposable());

		long pos = sut.Seek(2, SeekOrigin.Begin);

		Assert.Equal(2, pos);
		Assert.Equal(2, sut.Position);
		Assert.Equal(3, sut.ReadByte());
	}

	[Fact]
	public void Position_GetSet_DelegatesToInner()
	{
		byte[] data = [1, 2, 3, 4, 5];
		using var inner = new MemoryStream(data);
		using var sut = new GatekeptStream(inner, new TrackingDisposable());

		sut.Position = 3;
		Assert.Equal(3, sut.Position);
		Assert.Equal(4, sut.ReadByte());
	}

	[Fact]
	public void Length_DelegatesToInner()
	{
		byte[] data = [1, 2, 3];
		using var inner = new MemoryStream(data);
		using var sut = new GatekeptStream(inner, new TrackingDisposable());

		Assert.Equal(3, sut.Length);
	}

	[Fact]
	public void Dispose_DisposesInnerAndLease()
	{
		var inner = new MemoryStream();
		var lease = new TrackingDisposable();

		var sut = new GatekeptStream(inner, lease);
		sut.Dispose();

		Assert.True(inner is not null); // just ensure no exception
		Assert.True(lease.WasDisposed);
	}

	[Fact]
	public void Dispose_Idempotent()
	{
		var inner = new TrackingStream();
		var lease = new TrackingDisposable();

		var sut = new GatekeptStream(inner, lease);
		sut.Dispose();
		sut.Dispose();

		Assert.Equal(1, inner.DisposeCallCount);
		Assert.True(lease.WasDisposed);
	}

	private sealed class TrackingDisposable : IDisposable
	{
		public bool WasDisposed { get; private set; }
		public void Dispose() => WasDisposed = true;
	}

	private sealed class TrackingStream : MemoryStream
	{
		public int DisposeCallCount { get; private set; }
		protected override void Dispose(bool disposing)
		{
			DisposeCallCount++;
			base.Dispose(disposing);
		}
	}
}
