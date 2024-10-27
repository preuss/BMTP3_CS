using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace BMTP3.Core.Injection {
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
			ArgumentNullException.ThrowIfNull(func);
			_serviceCollection.AddSingleton(service, (provider) => func());
		}
	}
}
