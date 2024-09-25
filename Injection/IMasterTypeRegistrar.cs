using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
namespace BMTP3_CS.Injection {
	internal interface IMasterTypeRegistrar : ITypeRegistrar {
		/// <summary>
		/// Registers the specified service.
		/// </summary>
		/// <param name="service">The service.</param>
		/// <param name="implementation">The implementation.</param>
		void Register(Type service, Type implementation);

		/// <summary>
		/// Registers the specified instance.
		/// </summary>
		/// <param name="service">The service.</param>
		/// <param name="implementation">The implementation.</param>
		void RegisterInstance(Type service, object implementation);

		/// <summary>
		/// Registers the specified instance lazily.
		/// </summary>
		/// <param name="service">The service.</param>
		/// <param name="factory">The factory that creates the implementation.</param>
		void RegisterLazy(Type service, Func<object> factory);

		/// <summary>
		/// Builds the type resolver representing the registrations
		/// specified in the current instance.
		/// </summary>
		/// <returns>A type resolver.</returns>
		ITypeResolver Build();
	}
}
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
