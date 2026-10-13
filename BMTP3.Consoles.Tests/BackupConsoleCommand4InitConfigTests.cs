using BMTP3.Consoles.ConsoleCommands;

namespace BMTP3.Consoles.Tests;

public class BackupConsoleCommand4InitConfigTests
{
	private static readonly string[] ExpectedHashAlgorithms =
	[
		"sha3-512", "sha3-256", "keccak-512", "keccak-256",
		"sha512", "sha256", "md5", "blake3-256", "blake3-512"
	];

	public static TheoryData<string, string> TemplateGenerators => new()
	{
		{ "toml", BackupConsoleCommand4InitConfig.GenerateTomlContent() },
		{ "json", BackupConsoleCommand4InitConfig.GenerateJsonContent() },
		{ "json5", BackupConsoleCommand4InitConfig.GenerateJson5Content() }
	};

	[Theory]
	[MemberData(nameof(TemplateGenerators))]
	public void GeneratedConfig_DeclaresTheSameDefaultsAsTheBuilder(string format, string content)
	{
		string path = Path.Combine(Path.GetTempPath(), $"init_config_test_{Guid.NewGuid():N}.{format}");
		try
		{
			File.WriteAllText(path, content);

			Configs.BackupPlan4Config config = Configs.BackupPlan4Loader.Load(new FileInfo(path));

			// Drift guard: templates must declare the same standard as the CLI/builder defaults.
			Assert.Equal("persistent", config.Behavior.ItemIdScope);
			Assert.Equal(ExpectedHashAlgorithms, config.Metadata.ComparisonHashAlgorithms);
			Assert.Equal(ExpectedHashAlgorithms, config.Metadata.VerificationHashAlgorithms);
		}
		finally
		{
			File.Delete(path);
		}
	}
}
