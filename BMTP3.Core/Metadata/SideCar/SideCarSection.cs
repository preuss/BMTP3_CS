using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Metadata.SideCar {
    /// <summary>
    /// Represents a section in a SideCar document
    /// </summary>
    public sealed class SideCarSection {
        public string Name { get; }
        public int Weight { get; }
        public List<SideCarProperty> Properties { get; } = new();

        /// <summary>
        /// Creates a new SideCar section
        /// </summary>
        /// <param name="name">Name of the section</param>
        /// <param name="weight">Weight for sorting (default: 100)</param>
        public SideCarSection(string name, int weight = 100) {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            
            Name = name;
            Weight = weight;
        }

        /// <summary>
        /// Adds a property to the section
        /// </summary>
        /// <param name="property">Property to add</param>
        public void AddProperty(SideCarProperty property) {
            ArgumentNullException.ThrowIfNull(property);
            Properties.Add(property);
        }

		/// <summary>
		/// Returns properties so that:
		/// 1) All with Weight >= 0 come first (ordered by Weight asc, then Key)
		/// 2) All with Weight < 0 come last (ordered only by Key)
		/// </summary>
		public IEnumerable<SideCarProperty> GetSortedProperties() {
			return Properties
				.OrderBy(p => p.Weight < 0)                    // Non-negative first (false), negatives last (true)
				.ThenBy(p => p.Weight < 0 ? 0 : p.Weight)      // Only differentiates non-negative group
				.ThenBy(p => p.Key, StringComparer.Ordinal);   // Key ordering (tie-breaker; only key for negatives)
		}
	}
}
