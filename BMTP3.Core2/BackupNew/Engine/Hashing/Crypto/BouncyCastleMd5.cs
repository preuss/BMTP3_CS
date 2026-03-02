using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace BMTP3.Core2.Engine.Hashing.Crypto;
/// <summary>
/// Wrapper that adapts the BouncyCastle MD5Digest to the HashAlgorithm API.
/// Used where HashCalculator expects a HashAlgorithm.
/// </summary>
internal sealed class BouncyCastleMd5 : HashAlgorithm
{
	private MD5Digest _digest;

	public BouncyCastleMd5()
	{
		_digest = new MD5Digest();
		HashSizeValue = 128;
	}

	public override void Initialize()
	{
		_digest = new MD5Digest();      // Cheap reset (state is small)
		HashValue = null;
	}

	protected override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		if(cbSize > 0)
		{
			_digest.BlockUpdate(array, ibStart, cbSize);
		}
	}

	protected override byte[] HashFinal()
	{
		byte[] output = new byte[_digest.GetDigestSize()];
		_digest.DoFinal(output, 0);
		HashValue = output;
		return output;
	}
}
