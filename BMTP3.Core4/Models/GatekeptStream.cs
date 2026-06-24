namespace BMTP3.Core4.Models;

internal sealed class GatekeptStream : Stream
{
	private readonly Stream _inner;
	private readonly IDisposable _lease;
	private int _disposed;

	public GatekeptStream(Stream inner, IDisposable lease)
	{
		try
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_lease = lease ?? throw new ArgumentNullException(nameof(lease));
		}
		catch
		{
			inner?.Dispose();
			lease?.Dispose();
			throw;
		}
	}


	public override bool CanRead => _inner.CanRead;
	public override bool CanSeek => _inner.CanSeek;
	public override bool CanWrite => false;
	public override long Length => _inner.Length;

	public override long Position
	{
		get => _inner.Position;
		set => _inner.Position = value;
	}

	public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
	public override int Read(Span<byte> buffer) => _inner.Read(buffer);
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _inner.ReadAsync(buffer, offset, count, cancellationToken);
	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		=> _inner.ReadAsync(buffer, cancellationToken);

	public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
	public override void Flush() => _inner.Flush();

	public override void SetLength(long value) => throw new NotSupportedException("MTP streams are read-only.");
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("MTP streams are read-only.");

	protected override void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			if (disposing)
			{
				_inner.Dispose();
				_lease.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}
