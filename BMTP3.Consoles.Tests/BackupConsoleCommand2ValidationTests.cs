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
			SourceDirectory = null,
			// Ensure OutputDirectory is present so the validation fails on source next
			OutputDirectory = new System.IO.DirectoryInfo(@"D:\\Backups")
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


	// Helper methods to call private methods via reflection
	private static void CallValidate(BackupOptionsModel model)
	{
		MethodInfo? method = typeof(BackupConsoleCommand2).GetMethod("ValidateBackupOptions",
			BindingFlags.NonPublic | BindingFlags.Instance);
		BackupConsoleCommand2 command = new();
		command.GetType().GetProperty("ServiceProvider")?.SetValue(command, null);
		try
		{
			method?.Invoke(command, new object[] { model });
		}
		catch (TargetInvocationException tie) when (tie.InnerException != null)
		{
			// Rethrow the inner exception so Assert.Throws can match the expected type
			throw tie.InnerException;
		}
	}

}