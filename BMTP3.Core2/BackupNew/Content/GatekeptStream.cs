using BMTP3.Core2.BackupNew.Engine.Traversal;

namespace BMTP3.Core2.BackupNew.Content;

/// <summary>
/// A stream wrapper that holds an exclusive MTP gatekeeper lease for its entire lifetime.
///
/// ## Design rationale
///
/// MTP devices require single-threaded access. The naive approach — acquiring the gatekeeper
/// semaphore on every <see cref="Read"/> / <see cref="ReadAsync"/> call — is correct for
/// safety but creates enormous overhead: a typical file copy via <see cref="Stream.CopyToAsync"/>
/// uses ~80 KB chunks, so a 100 MB file would incur ~1 280 semaphore acquire/release cycles.
///
/// Instead, <see cref="MediaFileContent.OpenRead"/> acquires the semaphore <em>once</em> via
/// <see cref="IMtpGatekeeper.AcquireAsync"/> before opening the underlying MTP stream, and stores
/// the resulting <see cref="IDisposable"/> lease here.  All read calls are forwarded directly to
/// the inner stream — no per-read locking.  When the caller disposes this stream, the lease is
/// released, freeing the semaphore for the next operation.
///
/// This is safe because:
/// <list type="bullet">
///   <item>The semaphore remains held for the full duration of the file transfer.</item>
///   <item><see cref="ContentBufferingPipelineStage"/> runs with parallelism = 1, so only one
///         file is staged at a time — there is never a second concurrent caller waiting for the
///         device while a transfer is in progress.</item>
///   <item><see cref="Dispose"/> is idempotent; double-dispose does not double-release.</item>
/// </list>
/// </summary>
public sealed class GatekeptStream : Stream
{
	private readonly Stream _inner;
	private readonly IDisposable _lease;
	private bool _disposed;

	/// <param name="inner">The raw MTP stream opened while the lease was held.</param>
	/// <param name="lease">
	/// The lease returned by <see cref="IMtpGatekeeper.AcquireAsync"/>.
	/// Disposed together with this stream.
	/// </param>
	public GatekeptStream(Stream inner, IDisposable lease)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_lease = lease ?? throw new ArgumentNullException(nameof(lease));
	}

	// -----------------------------------------------------------------------
	// Stream capability pass-through
	// -----------------------------------------------------------------------

	public override bool CanRead  => _inner.CanRead;
	public override bool CanSeek  => _inner.CanSeek;
	public override bool CanWrite => false; // MTP streams are read-only
	public override long Length   => _inner.Length;

	public override long Position
	{
		get => _inner.Position;
		set => _inner.Position = value;
	}

	// -----------------------------------------------------------------------
	// Read operations — direct pass-through; semaphore already held via lease
	// -----------------------------------------------------------------------

	public override int Read(byte[] buffer, int offset, int count)
		=> _inner.Read(buffer, offset, count);

	public override int Read(Span<byte> buffer)
		=> _inner.Read(buffer);

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _inner.ReadAsync(buffer, offset, count, cancellationToken);

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		=> _inner.ReadAsync(buffer, cancellationToken);

	// -----------------------------------------------------------------------
	// Unsupported write operations
	// -----------------------------------------------------------------------

	public override void Flush() => _inner.Flush();

	public override void SetLength(long value)
		=> throw new NotSupportedException("MTP streams are read-only.");

	public override void Write(byte[] buffer, int offset, int count)
		=> throw new NotSupportedException("MTP streams are read-only.");

	public override long Seek(long offset, SeekOrigin origin)
		=> _inner.Seek(offset, origin);

	// -----------------------------------------------------------------------
	// Disposal — releases the MTP semaphore lease
	// -----------------------------------------------------------------------

	protected override void Dispose(bool disposing)
	{
		if(!_disposed)
		{
			if(disposing)
			{
				_inner.Dispose();
				_lease.Dispose(); // releases the semaphore exactly once
			}
			_disposed = true;
		}
		base.Dispose(disposing);
	}
}
