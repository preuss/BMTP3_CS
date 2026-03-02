using BMTP3.Core2.BackupNew.candidates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.definitions;
/// <summary>
/// Definition specifically for XMP which uses Strings (Namespace + Property Name).
/// </summary>
public record XmpTagDefinition(
	TimestampRole Role,
	string Namespace,
	string PropertyName
);