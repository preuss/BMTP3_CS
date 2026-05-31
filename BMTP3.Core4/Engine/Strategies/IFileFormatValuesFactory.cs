using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal interface IFileFormatValuesFactory
{
	Dictionary<string, object> Create(FileFormatValuesRequest request);
}
