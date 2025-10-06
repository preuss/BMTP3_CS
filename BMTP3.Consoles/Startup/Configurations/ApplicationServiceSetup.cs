using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Startup.Configurations;
public class ApplicationServiceSetup : IServiceSetup {
	public void Configure(IServiceCollection services, IConfiguration configuration) {
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSingleton<TimeProvider>(TimeProvider.System);


		services.AddTransient<ConsoleApplication>();

		// services.AddTransient<IMyLogic, MyLogic>();
	}
}