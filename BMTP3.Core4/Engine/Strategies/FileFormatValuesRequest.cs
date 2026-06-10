using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed record FileFormatValuesRequest(
	string FileName,
	string RelativeFilePath,
	DateTimeOffset CreateFileDate,
	string ItemId,
	string? StrongHash,
	string? DeviceName,
	string? DeviceModel
);