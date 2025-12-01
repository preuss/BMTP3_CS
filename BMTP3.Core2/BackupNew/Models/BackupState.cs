using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Definerer alle mulige tilstande et IBackupItem kan befinde sig i.
/// Rækkefølgen er vigtig – vi bruger den til validering i BackupItem.AdvanceTo().
/// </summary>
public enum BackupState {
	/// <summary>
	/// Nyoprettet – intet er gjort endnu.
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Filen er downloadet/kopieret til lokal temp-mappe (hvis nødvendigt).
	/// Content peger nu på en lokal fil.
	/// </summary>
	Staged = 10,

	/// <summary>
	/// Hash er beregnet, EXIF/metadata udtrukket, osv.
	/// </summary>
	Analyzed = 20,

	/// <summary>
	/// Beslutning truffet: Skal kopieres, springes over pga. duplikat, osv.
	/// </summary>
	Decided = 30,

	/// <summary>
	/// Filen er kopieret til endelig destination + sidecar-fil skrevet.
	/// Item er færdigt behandlet.
	/// </summary>
	Committed = 40,

	/// <summary>
	/// En fejl opstod – item fejlede, men processen fortsætter med andre filer.
	/// </summary>
	Failed = 90,

	/// <summary>
	/// Bevidst sprunget over (f.eks. allerede eksisterer med samme hash og dato).
	/// </summary>
	Skipped = 95
}