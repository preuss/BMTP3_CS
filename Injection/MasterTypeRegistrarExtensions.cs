using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3_CS.Injection {
	static class MasterTypeRegistrarExtensions {
		public static IMasterTypeRegistrar Register<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IMasterTypeRegistrar registrar)
			where TService : class
			where TImplementation : class, TService {
			registrar.Register(typeof(TService), typeof(TImplementation));
			return registrar;
		}
		public static IMasterTypeRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IMasterTypeRegistrar registrar) {
			registrar.Register(typeof(TService), typeof(TService));
			return registrar;
		}
		public static IMasterTypeRegistrar RegisterInstance<TService>(this IMasterTypeRegistrar registrar, object implementationInstance) {
			registrar.RegisterInstance(typeof(TService), implementationInstance);
			return registrar;
		}

	}
}
