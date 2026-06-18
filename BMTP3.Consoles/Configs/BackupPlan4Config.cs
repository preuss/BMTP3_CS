namespace BMTP3.Consoles.Configs;

public sealed class BackupPlan4Config
{
	public string Name { get; set; } = string.Empty;
	public SourceConfig Source { get; set; } = new();
	public DestinationConfig Destination { get; set; } = new();
	public CollisionConfig Collision { get; set; } = new();
	public MetadataConfig Metadata { get; set; } = new();
	public BehaviorConfig Behavior { get; set; } = new();
}

public sealed class SourceConfig
{
	public string Type { get; set; } = "filesystem";
	public string Path { get; set; } = string.Empty;
	public bool Recursive { get; set; } = true;
	public List<string> IncludePatterns { get; set; } = new();
	public List<string> ExcludePatterns { get; set; } = new();
}

public sealed class DestinationConfig
{
	public string Path { get; set; } = string.Empty;
	public string OutputStructure { get; set; } = "preserve-hierarchy";
	public string CustomOutputPattern { get; set; } = string.Empty;
}

public sealed class CollisionConfig
{
	public string Strategy { get; set; } = "rename";
	public string Comparison { get; set; } = "binary";
	public string RenameStrategy { get; set; } = "increment";
	public string CustomPattern { get; set; } = string.Empty;
}

public sealed class MetadataConfig
{
	public string SidecarFormat { get; set; } = "ini";
	public string IndexType { get; set; } = "none";
	public List<string> ComparisonHashAlgorithms { get; set; } = new();
	public List<string> VerificationHashAlgorithms { get; set; } = new();
	public string PostWriteVerification { get; set; } = "none";
	public bool EnableTimestampCorrection { get; set; } = true;
}

public sealed class BehaviorConfig
{
	public bool DryRun { get; set; }
	public bool StopOnError { get; set; } = true;
	public int Delay { get; set; }
	public string ResumeBehavior { get; set; } = "abort";
	public string ItemIdScope { get; set; } = "connection";
}
