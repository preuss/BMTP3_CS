using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class StreamHashGenerator_AdvancedTests
{
	[Fact]
	public async Task ComputeHashesAsync_CancelsWhenStreamSlow()
	{
		byte[] data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
		SlowReadStream slow = new(data, 200);

		NullLogger<StreamHashGenerator> logger = new();
		StreamHashGenerator gen = new(logger);

		using CancellationTokenSource cts = new(100); // cancel quickly

		await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			await gen.ComputeHashesAsync(slow, new[] { HashType.SHA2_256 }, null, cts.Token);
		});
	}

	[Fact]
	public async Task ComputeHashesAsync_WorksWithNonSeekableStream()
	{
		byte[] data = Encoding.UTF8.GetBytes("Lorem ipsum dolor sit amet");
		NonSeekableStream ns = new(data);

		NullLogger<StreamHashGenerator> logger = new();
		StreamHashGenerator gen = new(logger);

		Dictionary<HashType, string> result =
			await gen.ComputeHashesAsync(ns, new[] { HashType.SHA2_256 }, null, CancellationToken.None);
		Assert.NotNull(result);
		Assert.True(result.ContainsKey(HashType.SHA2_256));
		Assert.False(string.IsNullOrWhiteSpace(result[HashType.SHA2_256]));
	}

	private class SlowReadStream : Stream
	{
		private readonly int _delayMs;
		private readonly MemoryStream _inner;

		public SlowReadStream(byte[] data, int delayMs)
		{
			_inner = new MemoryStream(data);
			_delayMs = delayMs;
		}

		public override bool CanRead => true;
		public override bool CanSeek => _inner.CanSeek;
		public override bool CanWrite => false;
		public override long Length => _inner.Length;

		public override long Position
		{
			get => _inner.Position;
			set => _inner.Position = value;
		}

		public override void Flush()
		{
			_inner.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			// synchronous read delegates to inner
			return _inner.Read(buffer, offset, count);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
			CancellationToken cancellationToken)
		{
			await Task.Delay(_delayMs, cancellationToken);
			return await _inner.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _inner.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_inner.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}

	private class NonSeekableStream : Stream
	{
		private readonly Stream _inner;

		public NonSeekableStream(byte[] data)
		{
			_inner = new MemoryStream(data);
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
			_inner.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _inner.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _inner.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}
}