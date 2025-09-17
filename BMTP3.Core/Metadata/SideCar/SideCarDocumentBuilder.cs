using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Metadata.SideCar {
    /// <summary>
    /// Builder for creating SideCarDocument with fluent API
    /// </summary>
    public sealed class SideCarDocumentBuilder {
        private readonly SideCarDocument _document = new();
        private SideCarSection? _currentSection;

        /// <summary>
        /// Adds a new section and sets it as current
        /// </summary>
        /// <param name="name">Name of the section</param>
        /// <param name="weight">Weight for sorting (default: 100)</param>
        /// <returns>Builder instance for fluent chaining</returns>
        public SideCarDocumentBuilder AddSection(string name, int weight = 100) {
            _currentSection = new SideCarSection(name, weight);
            _document.AddSection(_currentSection);
            return this;
        }

        /// <summary>
        /// Adds a property to the current section
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        /// <param name="weight">Weight for sorting (default: 100)</param>
        /// <returns>Builder instance for fluent chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown if no section is active</exception>
        public SideCarDocumentBuilder AddProperty(string key, string? value, int weight = 100) {
            if (_currentSection == null) {
                throw new InvalidOperationException("A section must be added before properties can be added. Call AddSection() first.");
            }

            _currentSection.AddProperty(new SideCarProperty(key, value, weight));
            return this;
        }

        /// <summary>
        /// Adds a property with DateTime value
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">DateTime value (formatted with ToString("o"))</param>
        /// <param name="weight">Weight for sorting (default: 100)</param>
        /// <returns>Builder instance for fluent chaining</returns>
        public SideCarDocumentBuilder AddProperty(string key, DateTime? value, int weight = 100) {
            return AddProperty(key, value?.ToString("o"), weight);
        }

        /// <summary>
        /// Adds a property with numeric value
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Numeric value</param>
        /// <param name="weight">Weight for sorting (default: 100)</param>
        /// <returns>Builder instance for fluent chaining</returns>
        public SideCarDocumentBuilder AddProperty(string key, long value, int weight = 100) {
            return AddProperty(key, value.ToString(), weight);
        }

        /// <summary>
        /// Builds the completed SideCarDocument
        /// </summary>
        /// <returns>Complete SideCarDocument</returns>
        public SideCarDocument Build() {
            return _document;
        }

        /// <summary>
        /// Convenience method to start a new builder
        /// </summary>
        /// <returns>New SideCarDocumentBuilder instance</returns>
        public static SideCarDocumentBuilder Create() {
            return new SideCarDocumentBuilder();
        }
    }
}