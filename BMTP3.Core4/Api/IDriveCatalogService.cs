using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Api;

public interface IDriveCatalogService
{
	IReadOnlyList<DriveCatalogEntry> ListDrives();
}
