using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Infrastructure.Traversal;

namespace BMTP3.Core2.BackupNew.Content;

/// <summary>
/// A stream wrapper that ensures all read operations are guarded by the MTP Gatekeeper.
/// This guarantees exclusive access to the device during reads.
/// </summary>
public class GatekeptStream : Stream
{
    private readonly Stream _inner;
    private readonly IMtpGatekeeper _gatekeeper;
    private readonly CancellationToken _ct; // Default CT for operations not passing one

    public GatekeptStream(Stream inner, IMtpGatekeeper gatekeeper)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
    }

    // Pass-through properties
    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    public override void Flush() => _inner.Flush();

    // Guarded Read
    public override int Read(byte[] buffer, int offset, int count)
    {
        // For synchronous read, we unfortunately have to block async gatekeeper.
        // Ideally we use ReadAsync everywhere.
        return _gatekeeper.ExecuteAsync(() => Task.FromResult(_inner.Read(buffer, offset, count)), CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return await _gatekeeper.ExecuteAsync(() => _inner.ReadAsync(buffer, offset, count, cancellationToken), cancellationToken);
    }
    
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
         return await _gatekeeper.ExecuteAsync(() => _inner.ReadAsync(buffer, cancellationToken).AsTask(), cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _gatekeeper.ExecuteAsync(() => Task.FromResult(_inner.Seek(offset, origin)), CancellationToken.None).GetAwaiter().GetResult();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("MTP Stream is read-only");
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}
