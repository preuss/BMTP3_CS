using System.Reflection;
using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Consoles.Tests;

public class BackupConsoleCommand2ValidationTests
{
	// ----------------------------------------------------------------
	// ValidateBackupOptions tests
	// ----------------------------------------------------------------

	[Fact]
	public void ValidateBackupOptions_WithCustomPathPattern_MissingPathPattern_Throws()
	{
		BackupOptionsModel model = new()
		{
			OutputStrategy = OutputStructureStrategy.CustomPathPattern,
			CustomOutputFilePath = null
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--path-pattern", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithCustomCollisionPattern_MissingCollisionPattern_Throws()
	{
		BackupOptionsModel model = new()
		{
			RenameStrategy = RenameStrategy.CustomCollisionPathPattern,
			CustomCollisionOutputFilePath = null
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--collision-pattern", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithoutConfig_MissingSourceDirectory_Throws()
	{
		BackupOptionsModel model = new()
		{
			Config = null,
			SourceDirectory = null
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--source-directory", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithoutConfig_MissingOutput_Throws()
	{
		BackupOptionsModel model = new()
		{
			Config = null,
			OutputDirectory = null
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--output", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithNegativeDelay_Throws()
	{
		BackupOptionsModel model = new()
		{
			Delay = -1
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--delay", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithZeroVerifyRetryCount_Throws()
	{
		BackupOptionsModel model = new()
		{
			VerificationRetryCount = 0
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--verify-retry-count", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithNegativeVerifyRetryDelay_Throws()
	{
		BackupOptionsModel model = new()
		{
			VerificationRetryDelayMs = -1
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--verify-retry-delay", ex.Message);
	}

	[Fact]
	public void ValidateBackupOptions_WithNegativeVerifyTimeout_Throws()
	{
		BackupOptionsModel model = new()
		{
			VerificationTimeoutMs = -1
		};

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CallValidate(model));
		Assert.Contains("--verify-timeout", ex.Message);
	}

	// ----------------------------------------------------------------
	// EngineArgumentBuilder tests - Name logic
	// ----------------------------------------------------------------

	[Fact]
	public void EngineArgumentBuilder_Name_FromCLIName()
	{
		BackupOptionsModel model = new()
		{
			Name = "MyCustomName",
			OutputDirectory = new DirectoryInfo(@"D:\Backups")
		};

		BackupPlan plan = CallBuild(model);

		Assert.Equal("MyCustomName", plan.Name);
	}

	[Fact]
	public void EngineArgumentBuilder_Name_FromConfigFileName()
	{
		BackupOptionsModel model = new()
		{
			Name = null,
			Config = new FileInfo("iphone.toml"),
			OutputDirectory = new DirectoryInfo(@"D:\Backups")
		};

		BackupPlan plan = CallBuild(model);

		Assert.Equal("iphone", plan.Name);
	}

	[Fact]
	public void EngineArgumentBuilder_Name_FromSourceDevice()
	{
		BackupOptionsModel model = new()
		{
			Name = null,
			Config = null,
			SourceDevice = "Apple iPhone",
			OutputDirectory = new DirectoryInfo(@"D:\Backups")
		};

		BackupPlan plan = CallBuild(model);

		Assert.Equal("Apple iPhone", plan.Name);
	}

	[Fact]
	public void EngineArgumentBuilder_Name_FromOutputDirectory()
	{
		BackupOptionsModel model = new()
		{
			Name = null,
			Config = null,
			SourceDevice = null,
			SourceDirectory = @"C:\Photos",
			OutputDirectory = new DirectoryInfo(@"D:\MyBackups")
		};

		BackupPlan plan = CallBuild(model);

		Assert.Equal("MyBackups", plan.Name);
	}

	[Fact]
	public void EngineArgumentBuilder_Name_FallbackToBackup()
	{
		BackupOptionsModel model = new()
		{
			Name = null,
			Config = null,
			SourceDevice = null,
			SourceDirectory = @"C:\Photos",
			OutputDirectory = null
		};

		BackupPlan plan = CallBuild(model);

		Assert.Equal("backup", plan.Name);
	}

	// Helper methods to call private methods via reflection
	private static void CallValidate(BackupOptionsModel model)
	{
		MethodInfo? method = typeof(BackupConsoleCommand2).GetMethod("ValidateBackupOptions",
			BindingFlags.NonPublic | BindingFlags.Instance);
		BackupConsoleCommand2 command = new();
		command.GetType().GetProperty("ServiceProvider")?.SetValue(command, null);
		method?.Invoke(command, new object[] { model });
	}

	private static BackupPlan CallBuild(BackupOptionsModel model)
	{
		MethodInfo? method = typeof(BackupConsoleCommand2).GetMethod("EngineArgumentBuilder",
			BindingFlags.NonPublic | BindingFlags.Instance);
		BackupConsoleCommand2 command = new();
		command.GetType().GetProperty("ServiceProvider")?.SetValue(command, null);
		return (BackupPlan)method?.Invoke(command, new object[] { model })!;
	}
}