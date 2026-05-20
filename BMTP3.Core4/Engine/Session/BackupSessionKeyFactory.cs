using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Creates backup session state keys from backup plans.
/// </summary>
internal static class BackupSessionKeyFactory
{
	public static BackupSessionKey Create(BackupPlan plan)
	{
		plan = Guard.RequireNonNull(plan);

		string sessionId = Guid.NewGuid().ToString("N");
		string sourceIdentity = CreateSourceIdentity(plan);

		return new BackupSessionKey(sessionId, sourceIdentity);
	}

	private static string CreateSourceIdentity(BackupPlan plan)
	{
		return $"{plan.SourceType}:{plan.SourcePath.Trim()}";
	}
}