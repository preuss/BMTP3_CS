using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.Tests.Strategies;

public class SidecarReaderTests
{
	[Fact]
	public async Task Reads_Hashes_From_Json_Hashes_Object()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_json");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f.jpg");
		string sidecar = file + ".bmtp3.json";
		try
		{
			File.WriteAllText(file, "x");
			File.WriteAllText(sidecar, "{ \"hashes\": { \"SHA2_256\": \"deadbeef\" } }");

			string? val =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("deadbeef", val);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecar);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task Reads_Hashes_From_Ini_With_Comments_And_BlankLines_And_CRLF()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_ini_comments");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f5.jpg");
		string sidecar = Path.ChangeExtension(file, ".ini");
		try
		{
			File.WriteAllText(file, "x");
			// Include comments (# and ;) and blank lines and CRLF line endings
			string content = "# comment\r\nSHA2_256=aa11bb22\r\n; another comment\r\n\r\n";
			File.WriteAllText(sidecar, content);

			string? val =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("aa11bb22", val);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecar);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task Reads_Hashes_From_Json_With_UTF8_BOM_And_Falls_Back_On_Malformed_JSON_To_Ini()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_json_bom");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f6.jpg");
		string sidecar = file + ".bmtp3.json";
		try
		{
			File.WriteAllText(file, "x");
			// Malformed JSON (unclosed object) but we'll place a valid key=value later
			byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
			byte[] malformed = Encoding.UTF8.GetBytes("{ \"hashes\": { \"SHA2_256\": \"dead\"");
			using (FileStream fs = File.OpenWrite(sidecar))
			{
				fs.Write(bom, 0, bom.Length);
				fs.Write(malformed, 0, malformed.Length);
			}

			// Also write a .meta fallback INI so reader can still find it
			string meta = Path.ChangeExtension(file, ".meta");
			File.WriteAllText(meta, "SHA2_256=deadbeefini");

			string? val =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("deadbeefini", val);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecar);
			}
			catch
			{
			}

			try
			{
				File.Delete(Path.ChangeExtension(file, ".meta"));
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task Reads_Hashes_From_Ini_Format()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_ini");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f2.jpg");
		string sidecar = Path.ChangeExtension(file, ".meta");
		try
		{
			File.WriteAllText(file, "x");
			File.WriteAllText(sidecar, "SHA2_256=beefdead");

			string? val =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("beefdead", val);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecar);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task Reads_Hashes_From_Json_With_Varied_Casing_And_Root_Key()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_json_var");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f3.jpg");
		string sidecar = file + ".metadata.json";
		try
		{
			File.WriteAllText(file, "x");
			// Different casing for 'hashes' and algorithm key as SHA256 (legacy)
			File.WriteAllText(sidecar, "{ \"Hashes\": { \"SHA256\": \"cafebabe\" } }");

			string? val =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("cafebabe", val);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecar);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task Reads_Hashes_From_Json_DirectRoot_Key_And_Ini_Casing()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_json_root");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, "f4.jpg");
		string sidecarJson = file + ".json";
		string sidecarIni = Path.ChangeExtension(file, ".ini");
		try
		{
			File.WriteAllText(file, "x");
			File.WriteAllText(sidecarJson, "{ \"SHA2_256\": \"feedface\" }");
			File.WriteAllText(sidecarIni, "sha2_256=feedfaceini");

			string? valJson =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("feedface", valJson);

			// If JSON absent, INI casing should be accepted; simulate by removing JSON
			File.Delete(sidecarJson);
			string? valIni =
				await SidecarReader.TryReadHashFromSidecarAsync(file, HashType.SHA2_256, CancellationToken.None);
			Assert.Equal("feedfaceini", valIni);
		}
		finally
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecarJson);
			}
			catch
			{
			}

			try
			{
				File.Delete(sidecarIni);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}
}