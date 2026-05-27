using SharpHash.Base;
using SharpHash.Interfaces;
using System.Security.Cryptography;

namespace BMTP3.Core4.Hashing;

public class SharpHashSHA3_256 : HashAlgorithm
{
	private readonly IHash _hash;

	public SharpHashSHA3_256()
	{
		_hash = HashFactory.Crypto.CreateSHA3_256();
		HashSizeValue = _hash.HashSize;
		Initialize();
	}

	public override void Initialize()
	{
		_hash.Initialize();
	}

	protected override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		_hash.TransformBytes(array, ibStart, cbSize);
	}

	protected override byte[] HashFinal()
	{
		return _hash.TransformFinal().GetBytes();
	}
}
