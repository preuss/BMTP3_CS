using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public interface ISidecarGeneratorFactory
{
    ISidecarGenerator? Create(SidecarFormat format);
}
