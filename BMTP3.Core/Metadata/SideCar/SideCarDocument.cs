using System;
using System.Collections.Generic;
using System.Linq;

namespace BMTP3.Core.Metadata.SideCar {
	/// <summary>
	/// Represents a complete SideCar document with sections and properties
	/// </summary>
	public sealed class SideCarDocument {
		private readonly IDictionary<string, SideCarSection> _sections = new Dictionary<string, SideCarSection>();
		/// <summary>
		/// Gets all sections in the document
		/// </summary>
		public IEnumerable<SideCarSection> Sections => _sections.Values;
		/// <summary>
		/// Gets a section by name
		/// </summary>
		/// <param name="name">Section name</param>
		/// <returns>Section if found, null otherwise</returns>
		public SideCarSection? GetSection(string name) {
			_sections.TryGetValue(name, out SideCarSection? section);
			return section;
		}
		/// <summary>
		/// Adds a section to the document.
		/// Overwrites section, if name already exists.
		/// </summary>
		/// <param name="section">The section to add</param>
		public void AddSection(SideCarSection section) {
			ArgumentNullException.ThrowIfNull(section);
			
			_sections[section.Name] = section;
		}
		/// <summary>
		/// Gets existing section or creates new one if it doesn't exist
		/// </summary>
		/// <param name="name">Name of the section</param>
		/// <param name="weight">Weight for sorting if creating new section (default: 100)</param>
		/// <returns>Existing or newly created section</returns>
		public SideCarSection GetOrCreateSection(string name, int weight = 100) {
			if(_sections.TryGetValue(name, out SideCarSection? existingSection)) {
				return existingSection;
			}

			var newSection = new SideCarSection(name, weight);
			_sections[name] = newSection;
			return newSection;
		}
		/// <summary>
		/// Creates and adds a new section with fluent API
		/// </summary>
		/// <param name="name">Name of the section</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>The created section for fluent chaining</returns>
		public SideCarSection WithSection(string name, int weight = 100) {
			return GetOrCreateSection(name, weight);
		}

		/// <summary>
		/// Returns sections so that:
		/// 1) All with Weight gte 0 come first (ordered by Weight ascending, then Name)
		/// 2) All with Weight lt 0 come last (ordered only by Name; Weight is ignored inside this group)
		/// </summary>
		public IEnumerable<SideCarSection> GetSortedSections() {
			return Sections
				.OrderBy(s => s.Weight < 0)                  // Non-negative first (false), negative last (true)
				.ThenBy(s => s.Weight < 0 ? 0 : s.Weight)    // Only differentiates non-negative group
				.ThenBy(s => s.Name, StringComparer.Ordinal); // Final ordering by Name (also sole ordering for negative group)
		}
	}
}
