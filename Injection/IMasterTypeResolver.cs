using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Cli;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
namespace BMTP3_CS.Injection {
	internal interface IMasterTypeResolver : ITypeResolver {
		/// <summary>
		/// Resolves an instance of the specified type.
		/// </summary>
		/// <param name="type">The type to resolve.</param>
		/// <returns>An instance of the specified type, or <c>null</c> if no registration for the specified type exists.</returns>
		object? Resolve(Type? type);
	}
}
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
