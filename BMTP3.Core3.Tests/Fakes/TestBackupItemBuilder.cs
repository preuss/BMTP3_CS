namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Test builder for creating BackupItem instances with minimal setup.
/// </summary>
public class TestBackupItemBuilder
{
	private string _name = "test.txt";
	private string _sourcePath = "/source/test.txt";
	private string _destinationPath = "/dest/test.txt";
	private long _sizeInBytes = 1024;
	private DateTime _createdAt = DateTime.Now;
	private DateTime _modifiedAt = DateTime.Now;
	private Dictionary<string, object>? _metadata;
	private Dictionary<HashType, string>? _hashes;
	private string? _sidecarPath;
	private BackupItemType _type = BackupItemType.File;

	public TestBackupItemBuilder WithName(string name)
	{
		_name = name;
		return this;
	}

	public TestBackupItemBuilder WithSourcePath(string path)
	{
		_sourcePath = path;
		return this;
	}

	public TestBackupItemBuilder WithDestinationPath(string path)
	{
		_destinationPath = path;
		return this;
	}

	public TestBackupItemBuilder WithSizeInBytes(long size)
	{
		_sizeInBytes = size;
		return this;
	}

	public TestBackupItemBuilder WithMetadata(Dictionary<string, object> metadata)
	{
		_metadata = metadata;
		return this;
	}

	public TestBackupItemBuilder WithHashes(Dictionary<HashType, string> hashes)
	{
		_hashes = hashes;
		return this;
	}

	public TestBackupItemBuilder WithSidecarPath(string sidecarPath)
	{
		_sidecarPath = sidecarPath;
		return this;
	}

	public TestBackupItemBuilder WithType(BackupItemType type)
	{
		_type = type;
		return this;
	}

	public BackupItem Build()
	{
		return new BackupItem
		{
			Name = _name,
			SourcePath = _sourcePath,
			DestinationPath = _destinationPath,
			SizeInBytes = _sizeInBytes,
			CreatedAt = _createdAt,
			ModifiedAt = _modifiedAt,
			Metadata = _metadata,
			Hashes = _hashes,
			SidecarPath = _sidecarPath,
			Type = _type
		};
	}

	public static implicit operator BackupItem(TestBackupItemBuilder builder) => builder.Build();
}
