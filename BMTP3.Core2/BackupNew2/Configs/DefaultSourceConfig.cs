namespace BMTP3.Core2.Configs
{
	public class DefaultSourceConfig
	{
		public bool Disabled { get; set; } = true;
		public bool CompareByBinary { get; set; } = true;
		public bool Recursive { get; set; } = false;
		public bool? UseFilePattern { get; set; } = false;
		public string FilePattern { get; set; } = string.Empty;
		public string FilePatternIfExist { get; set; } = string.Empty;
		public string FolderOutput { get; set; } = string.Empty;
	}
}
