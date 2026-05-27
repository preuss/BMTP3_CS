using Blake3;
using System.Security.Cryptography;

namespace BMTP3.Core4.Hashing;

internal sealed class Blake3HashAlgorithm : HashAlgorithm
{
	private readonly int _outputBytes;
	private Hasher _hasher;
	private bool _initialized;

	public Blake3HashAlgorithm(int outputBytes)
	{
		if(outputBytes != 32 && outputBytes != 64)
		{
			throw new ArgumentOutOfRangeException(nameof(outputBytes));
		}

		_outputBytes = outputBytes;
		HashSizeValue = _outputBytes * 8;
		Initialize();
	}

	public override void Initialize()
	{
		if(_initialized)
		{
			_hasher.Dispose();
		}

		_hasher = Hasher.New();
		_initialized = true;
		HashValue = null;
	}

	protected override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		if(cbSize <= 0)
		{
			return;
		}

		_hasher.Update(array.AsSpan(ibStart, cbSize));
	}

	protected override byte[] HashFinal()
	{
		Span<byte> outBuf = stackalloc byte[64];
		_hasher.Finalize(outBuf);
		byte[] result = outBuf.Slice(0, _outputBytes).ToArray();
		HashValue = result;
		return result;
	}

	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			if(_initialized)
			{
				_hasher.Dispose();
				_initialized = false;
			}
		}

		base.Dispose(disposing);
	}
}
