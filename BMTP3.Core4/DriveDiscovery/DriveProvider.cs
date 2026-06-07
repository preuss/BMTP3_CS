using BMTP3.Core4.Storage;

namespace BMTP3.Core4.DriveDiscovery;

internal sealed class DriveProvider : IDriveProvider
{
	private readonly IReadOnlyList<IDriveProvider> _providers;

	public DriveProvider(IEnumerable<IDriveProvider> providers)
	{
		_providers = providers.ToList();
	}

	public IReadOnlyList<IBackupDriveInfo> ListDrives()
	{
		return _providers
			.SelectMany(p => p.ListDrives())
			.ToList();
	}
}