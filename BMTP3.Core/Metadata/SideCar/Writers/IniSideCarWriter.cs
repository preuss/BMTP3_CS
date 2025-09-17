using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Metadata.SideCar.Writers {
    /// <summary>
    /// Writer that generates INI format SideCar files
    /// </summary>
    public sealed class IniSideCarWriter : ISideCarWriter {
        /// <summary>
        /// Writes SideCarDocument to INI file
        /// </summary>
        /// <param name="document">Document to write</param>
        /// <param name="filePath">Path to output file</param>
        public void WriteToFile(SideCarDocument document, string filePath) {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            string content = WriteToString(document);
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        /// <summary>
        /// Converts SideCarDocument to INI format string
        /// </summary>
        /// <param name="document">Document to convert</param>
        /// <returns>INI format string</returns>
        public string WriteToString(SideCarDocument document) {
            ArgumentNullException.ThrowIfNull(document);

            StringBuilder sb = new();
            
            foreach (SideCarSection section in document.GetSortedSections()) {
                // Write section header
                sb.AppendLine($"[{section.Name}]");
                
                // Write properties for this section
                foreach (SideCarProperty property in section.GetSortedProperties()) {
                    sb.AppendLine($"{property.Key}={property.Value}");
                }
                
                // Add empty line after each section (except the last)
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}