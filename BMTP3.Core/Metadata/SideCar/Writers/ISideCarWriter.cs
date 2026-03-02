namespace BMTP3.Core.Metadata.SideCar.Writers {
	/// <summary>
	/// Interface for SideCar document writers
	/// </summary>
	public interface ISideCarWriter {
		/// <summary>
		/// Writes SideCarDocument to the specified file
		/// </summary>
		/// <param name="document">Document to write</param>
		/// <param name="filePath">Path to output file</param>
		void WriteToFile(SideCarDocument document, string filePath);

		/// <summary>
		/// Converts SideCarDocument to string format
		/// </summary>
		/// <param name="document">Document to convert</param>
		/// <returns>String representation of the document</returns>
		string WriteToString(SideCarDocument document);
	}
}