using SharpHash.Base;
using SharpHash.Interfaces;
using System.Security.Cryptography;

namespace BMTP3.Core.Handlers.Crypto {
	public class SharpHashSHA3_512 : HashAlgorithm {
		private readonly IHash _hash;

		public SharpHashSHA3_512() {
			_hash = HashFactory.Crypto.CreateSHA3_512();
			HashSizeValue = _hash.HashSize;
			Initialize();
		}

		public override void Initialize() {
			_hash.Initialize();
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			_hash.TransformBytes(array, ibStart, cbSize);
		}

		protected override byte[] HashFinal() {
			return _hash.TransformFinal().GetBytes();
		}
	}
}
