using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace BMTP3_CS.Injection {
	public sealed class TypeResolver : IMasterTypeResolver, IDisposable {
		private readonly IServiceProvider _provider;
		public TypeResolver(IServiceProvider provider) {
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}
		public object Resolve<T>() {
			object? resolvedObject = _provider.GetService(typeof(T));
			if(resolvedObject == null) {
				throw new InvalidOperationException($"Resolved type '{typeof(T)}', is null");
			}
			return resolvedObject;
		}
		public object? Resolve(Type? type) {
			if(type == null) {
				return null;
			}
			return _provider.GetService(type)!;
		}
		public void Dispose() {
			if(_provider is IDisposable disposable) {
				disposable.Dispose();
			}
		}
	}
}
