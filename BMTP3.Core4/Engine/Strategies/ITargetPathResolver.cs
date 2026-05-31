using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal interface ITargetPathResolver
{
	string Resolve(TargetPathResolveRequest request);
}
