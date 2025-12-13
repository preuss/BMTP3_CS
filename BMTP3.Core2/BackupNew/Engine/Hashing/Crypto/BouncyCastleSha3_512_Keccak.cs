using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.Engine.Hashing.Crypto;
internal sealed class BouncyCastleSha3_512_Keccak : HashAlgorithm
{
	private readonly KeccakDigest _digest = new(512);
	private bool _finalized;
	private byte[]? _hash;
	public BouncyCastleSha3_512_Keccak() { HashSizeValue = 512; }
	public override void Initialize()
	{
		_digest.Reset();
		_finalized = false;
		_hash = null;
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
		if(_finalized && _hash != null)
		{
			return _hash;
		}
		_hash = new byte[_digest.GetDigestSize()];
		_digest.DoFinal(_hash, 0);
		_finalized = true;
		return _hash;
	}
}
