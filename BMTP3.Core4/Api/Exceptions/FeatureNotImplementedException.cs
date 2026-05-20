namespace BMTP3.Core4.Api.Exceptions;

/// <summary>
/// Thrown when a <see cref="BackupPlan"/> requests a feature that belongs
/// to a Tier not yet implemented in the current build.
/// </summary>
public class FeatureNotImplementedException : BackupPlanValidationException
{
	/// <summary>
	/// The feature Tier number (2, 3, 4, etc.) that is not yet implemented.
	/// </summary>
	public int FeatureTier { get; }

	/// <summary>
	/// The name of the feature that is not yet implemented.
	/// </summary>
	public string FeatureName { get; }

	public FeatureNotImplementedException(int featureTier, string featureName)
		: base($"Feature '{featureName}' (Tier {featureTier}) is not implemented in this build.")
	{
		FeatureTier = featureTier;
		FeatureName = featureName;
	}
}
