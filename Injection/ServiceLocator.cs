using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3_CS.Injection {
	internal class ServiceLocator {
		private static IServiceProvider? _serviceProvider;
		private static IServiceProvider ServiceProvider {
			get {
				if(_serviceProvider == null) {
					throw new InvalidOperationException("ServiceLocator is not configured. Call Configure() with a valid IServiceProvider.");
				}
				return _serviceProvider;
			}
		}
		public static void Configure(IServiceProvider serviceProvider) {
			if(_serviceProvider != null) {
				throw new InvalidOperationException("ServiceLocator is already configured.");
			}
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		public static void ConfigureOverride(IServiceProvider serviceProvider) {
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		public static T GetService<T>() {
			return ServiceProvider.GetService<T>() ?? throw new InvalidOperationException($"Service of type {typeof(T).FullName} could not be retrieved.");
		}
		public static string ApplicationName { get; set; } = "My Application";
		public static string ApplicationVersion { get; set; } = "0.0.1";
	}
}
