using BMTP3.Core.BackupNew.Content;
using BMTP3.Core.BackupNew.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Models;
/// <summary>
/// Det centrale dataobjekt, der repræsenterer en enkelt fil i hele backup-processen.
/// Objektet "rejser" gennem alle stadier og bliver løbende beriget med data.
/// </summary>
public interface IBackupItem {
	/// <summary>
	/// Adgang til filens aktuelle indhold (kan skifte fra MTP-stream til lokal temp-fil osv.)
	/// </summary>
	ISourceContent Content { get; }

	/// <summary>
	/// Samling af al metadata – navn, sti, hashes, timestamps, osv.
	/// Beriges af hvert stage i pipelinen.
	/// </summary>
	Metadata Metadata { get; }

	/// <summary>
	/// Nuværende tilstand i backup-processen.
	/// </summary>
	BackupState State { get; }

	/// <summary>
	/// Fejlinformation for dette specifikke item.
	/// Tillader at et enkelt item fejler uden at stoppe hele backuppen.
	/// </summary>
	ErrorInfo ErrorInfo { get; }
}