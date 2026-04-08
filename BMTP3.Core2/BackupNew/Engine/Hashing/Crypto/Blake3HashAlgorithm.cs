using System.Security.Cryptography;
using Blake3;

namespace BMTP3.Core2.Engine.Hashing.Crypto;

/// <summary>
///     Adapter that exposes the Blake3 Hasher as a System.Security.Cryptography.HashAlgorithm.
///     Supports 256-bit (32 bytes) and 512-bit (64 bytes) outputs.
/// </summary>
internal sealed class Blake3HashAlgorithm : HashAlgorithm
{
	private readonly int _outputBytes;
	private Hasher _hasher;
	private bool _initialized;

	public Blake3HashAlgorithm(int outputBytes)
	{
		if (outputBytes != 32 && outputBytes != 64)
		{
			throw new ArgumentOutOfRangeException(nameof(outputBytes));
		}

		_outputBytes = outputBytes;
		HashSizeValue = _outputBytes * 8;
		Initialize();
	}

	public override void Initialize()
	{
		if (_initialized)
		{
			_hasher.Dispose();
		}

		_hasher = Hasher.New();
		_initialized = true;
		HashValue = null;
	}

	protected override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		if (cbSize <= 0)
		{
			return;
		}

		// Hasher.Update accepts ReadOnlySpan<byte>
		_hasher.Update(array.AsSpan(ibStart, cbSize));
	}

	protected override byte[] HashFinal()
	{
		Span<byte> outBuf = stackalloc byte[64]; // Blake3 can output up to 64 bytes here
		// Call the Span-based Finalize overload
		_hasher.Finalize(outBuf);
		byte[] result = outBuf.Slice(0, _outputBytes).ToArray();
		HashValue = result;
		return result;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_initialized)
			{
				_hasher.Dispose();
				_initialized = false;
			}
		}

		base.Dispose(disposing);
	}
}