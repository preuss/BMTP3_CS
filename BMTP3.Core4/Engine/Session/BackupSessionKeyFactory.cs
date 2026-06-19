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

		// SessionId is a stable, deterministic hash of the plan identity
		// (source type + source path + destination) so the same plan always
		// finds the same summary file — enabling resume across runs.
		// Different plans (different source or destination) get different IDs,
		// preventing cross-plan summary corruption.
		string sessionId = CreateStableSessionId(sourceIdentity);

		return new BackupSessionKey(sessionId, sourceIdentity);
	}

	private static string CreateSourceIdentity(BackupPlan plan)
	{
		return $"{plan.SourceType}:{plan.SourcePath.Trim()}:{plan.Destination.Trim()}";
	}

	private static string CreateStableSessionId(string sourceIdentity)
	{
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentity));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}
}