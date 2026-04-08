using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;

namespace BMTP3.Core2.Tests.BackupNew.Hashing;

public class HashCalculatorTests
{
	private readonly HashCalculator _calc = new();

	// Known vectors
	// SHA256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
	// SHA512("hello") = 9b71d224bd62f3785d96d46ad3ea3d73319bfbc2890caadae2dff72519673ca72323c3d99ba5c11d7c7acc6e14b8c5da0c4663475c2e5c3adef46f73bcdec043
	// MD5("hello")   = 5d41402abc4b2a76b9719d911017c592
	// SHA256("")      = e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855

	private static string WriteTempFile(byte[] data)
	{
		string path = Path.GetTempFileName();
		File.WriteAllBytes(path, data);
		return path;
	}

	[Fact]
	public void ComputeHashes_SHA2_256_ReturnsCorrectHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.SHA2_256 });

			Assert.True(result.ContainsKey(HashType.SHA2_256));
			Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", result[HashType.SHA2_256]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_SHA2_512_ReturnsCorrectHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.SHA2_512 });

			Assert.True(result.ContainsKey(HashType.SHA2_512));
			Assert.Equal(
				"9b71d224bd62f3785d96d46ad3ea3d73319bfbc2890caadae2dff72519673ca72323c3d99ba5c11d7c7acc6e14b8c5da0c4663475c2e5c3adef46f73bcdec043",
				result[HashType.SHA2_512]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_MD5_128_ReturnsCorrectHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.MD5_128 });

			Assert.True(result.ContainsKey(HashType.MD5_128));
			Assert.Equal("5d41402abc4b2a76b9719d911017c592", result[HashType.MD5_128]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_BLAKE3_256_ReturnsNonEmptyHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.BLAKE3_256 });

			Assert.True(result.ContainsKey(HashType.BLAKE3_256));
			string hash = result[HashType.BLAKE3_256];
			Assert.NotEmpty(hash);
			// BLAKE3-256 produces 32 bytes = 64 hex chars
			Assert.Equal(64, hash.Length);
			Assert.Matches("^[0-9a-f]+$", hash);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_BLAKE3_512_ReturnsNonEmptyHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.BLAKE3_512 });

			Assert.True(result.ContainsKey(HashType.BLAKE3_512));
			string hash = result[HashType.BLAKE3_512];
			Assert.NotEmpty(hash);
			// BLAKE3-512 produces 64 bytes = 128 hex chars
			Assert.Equal(128, hash.Length);
			Assert.Matches("^[0-9a-f]+$", hash);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_MultipleTypes_ReturnsAllRequested()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			HashType[] types = new[] { HashType.SHA2_256, HashType.MD5_128 };
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, types);

			Assert.True(result.ContainsKey(HashType.SHA2_256));
			Assert.True(result.ContainsKey(HashType.MD5_128));
			Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", result[HashType.SHA2_256]);
			Assert.Equal("5d41402abc4b2a76b9719d911017c592", result[HashType.MD5_128]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_EmptyStream_ReturnsKnownHash()
	{
		// SHA256 of empty input
		string tempFile = WriteTempFile(Array.Empty<byte>());
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.SHA2_256 });

			Assert.True(result.ContainsKey(HashType.SHA2_256));
			Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result[HashType.SHA2_256]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_UnknownHashType_ThrowsNotSupportedException()
	{
		// Use a cast to an undefined enum value to simulate an unsupported hash type
		HashType unsupported = (HashType)999;
		string tempFile = WriteTempFile(Encoding.UTF8.GetBytes("hello"));
		try
		{
			Assert.Throws<NotSupportedException>(() =>
				_calc.ComputeHashes(tempFile, new[] { unsupported }));
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_LargeInput_WorksCorrectly()
	{
		// 10 MB of zeros
		byte[] data = new byte[10 * 1024 * 1024];
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result = _calc.ComputeHashes(tempFile, new[] { HashType.SHA2_256 });

			Assert.True(result.ContainsKey(HashType.SHA2_256));
			Assert.NotEmpty(result[HashType.SHA2_256]);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_SHA3_256_FIPS202_ReturnsNonEmptyHex()
	{
		byte[] data = Encoding.UTF8.GetBytes("hello");
		string tempFile = WriteTempFile(data);
		try
		{
			IReadOnlyDictionary<HashType, string> result =
				_calc.ComputeHashes(tempFile, new[] { HashType.SHA3_256_FIPS202 });

			Assert.True(result.ContainsKey(HashType.SHA3_256_FIPS202));
			string hash = result[HashType.SHA3_256_FIPS202];
			Assert.NotEmpty(hash);
			// SHA3-256 produces 32 bytes = 64 hex chars
			Assert.Equal(64, hash.Length);
			Assert.Matches("^[0-9a-f]+$", hash);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ComputeHashes_NullHashTypes_ThrowsArgumentException()
	{
		string tempFile = WriteTempFile(Encoding.UTF8.GetBytes("hello"));
		try
		{
			Assert.Throws<ArgumentException>(() =>
				_calc.ComputeHashes(tempFile, null!));
		}
		finally
		{
			File.Delete(tempFile);
		}
	}
}