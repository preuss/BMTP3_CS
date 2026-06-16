using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Creates backup session state keys from backup plans.
/// </summary>
internal static class BackupSessionKeyFactory
{
	public static BackupSessionKey Create(BackupPlan plan)
	{
		plan = Guard.RequireNonNull(plan);

		string sourceIdentity = CreateSourceIdentity(plan);

		// SessionId is a stable, deterministic hash of the source identity so the same
		// summary file is found on every run for the same source — enabling resume.
		string sessionId = CreateStableSessionId(sourceIdentity);
		//string sessionId = Guid.NewGuid().ToString("N"); // Alternative: use a random session ID if you want to disable resume and always start fresh.

		return new BackupSessionKey(sessionId, sourceIdentity);
	}

	private static string CreateSourceIdentity(BackupPlan plan)
	{
		return $"{plan.SourceType}:{plan.SourcePath.Trim()}";
	}

	private static string CreateStableSessionId(string sourceIdentity)
	{
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentity));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}
}