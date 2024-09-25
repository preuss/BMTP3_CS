using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace BMTP3_CS.Injection {
	public sealed class TypeRegistrar : IMasterTypeRegistrar {
		private readonly IServiceCollection _serviceCollection;
		private static int globalCounter;
		private int counter;
		public TypeRegistrar(IServiceCollection serviceCollection) {
			_serviceCollection = serviceCollection;
			counter = globalCounter++;
		}
		public ITypeResolver Build() {
			return new TypeResolver(_serviceCollection.BuildServiceProvider());
		}
		public void Register(Type service, Type implementation) {
			_serviceCollection.AddSingleton(service, implementation);
		}
		public void RegisterInstance(Type service, object implementation) {
			_serviceCollection.AddSingleton(service, implementation);
		}
		public void RegisterLazy(Type service, Func<object> func) {
			if(func is null) {
				throw new ArgumentNullException(nameof(func));
			}
			_serviceCollection.AddSingleton(service, (provider) => func());
		}
	}
}
