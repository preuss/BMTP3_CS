using System;
using System.Collections.Generic;
using System.Linq;

namespace BMTP3.Core.Metadata.SideCar {
    /// <summary>
    /// Represents a complete SideCar document with sections and properties
    /// </summary>
    public sealed class SideCarDocument {
        public List<SideCarSection> Sections { get; } = new();

        /// <summary>
        /// Adds a section to the document
        /// </summary>
        /// <param name="section">The section to add</param>
        public void AddSection(SideCarSection section) {
            ArgumentNullException.ThrowIfNull(section);
            Sections.Add(section);
        }

        /// <summary>
        /// Returns sections so that:
        /// 1) All with Weight >= 0 come first (ordered by Weight ascending, then Name)
        /// 2) All with Weight < 0 come last (ordered only by Name; Weight is ignored inside this group)
        /// </summary>
        public IEnumerable<SideCarSection> GetSortedSections() {
            return Sections
                .OrderBy(s => s.Weight < 0)                  // Non-negative first (false), negative last (true)
                .ThenBy(s => s.Weight < 0 ? 0 : s.Weight)    // Only differentiates non-negative group
                .ThenBy(s => s.Name, StringComparer.Ordinal); // Final ordering by Name (also sole ordering for negative group)
        }
    }
}
