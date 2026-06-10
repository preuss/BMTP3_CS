using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Models;
/// <summary>
/// The central abstraction representing an item to be backed up in the backup session.
/// This interface allows for different types of backup items (e.g., files, directories, registry keys)
/// to be processed uniformly by the backup engine.
/// </summary>
internal interface IBackupItem
{
	/// <summary>
	/// Unique identifier for this item within the backup session.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// The original source path of the item. For file-based items, this is the full file path.
	/// </summary>
	string SourcePath { get; }

	/// <summary>
	/// The path of the item relative to the configured backup source.
	/// </summary>
	string RelativeFilePath { get; }

	/// <summary>
	/// The name of the file or directory represented by this backup item.
	/// </summary>
	string FileName { get; }

	/// <summary>
	///     The physical content source (file system file, MTP object, etc.).
	///     May change from an MTP stream to a local temp file during processing.
	/// </summary>
	IContent Content { get; }


	/// <summary>
	/// The source item creation date, if available.
	/// <para>
	/// For file system sources, this typically maps to file creation time.
	/// For MTP sources, this typically maps to <c>CreationTime</c>.
	/// </para>
	/// </summary>
	DateTimeOffset? DateCreated { get; set; }

	/// <summary>
	/// The source item last modified date, if available.
	/// <para>
	/// For file system sources, this typically maps to last write time.
	/// For MTP sources, this typically maps to <c>LastWriteTime</c>.
	/// </para>
	/// </summary>
	DateTimeOffset? DateModified { get; set; }

	/// <summary>
	/// The source item authored date, if available.
	/// <para>
	/// For media files, this may represent when the content was originally captured or authored.
	/// For MTP sources, this typically maps to <c>DateAuthored</c>.
	/// </para>
	/// </summary>
	DateTimeOffset? DateAuthored { get; set; }

	/// <summary>
	/// The source item last accessed date, if available.
	/// <para>
	/// Not all source types provide this value.
	/// </para>
	/// </summary>
	DateTimeOffset? DateAccessed { get; set; }
	
	/// <summary>
	/// Replaces the current content provider with a new one.
	/// Used when the backup engine needs to switch from an MTP stream to a local temporary file for processing.
	/// </summary>
	/// <param name="content">The new content provider.</param>
	void ReplaceContentProvider(IContent content);
}
