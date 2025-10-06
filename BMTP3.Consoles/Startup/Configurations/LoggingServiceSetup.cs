using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Startup.Configurations;
public class LoggingServiceSetup : IServiceSetup {
	public void Configure(IServiceCollection services, IConfiguration configuration) {
		services.AddLogging(loggingBuilder => {
			//loggingBuilder.ClearProviders();

			loggingBuilder.AddConsole();

			loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
			
			loggingBuilder.SetMinimumLevel(LogLevel.Warning);

			loggingBuilder.AddFilter("System", LogLevel.Warning);
			loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
			loggingBuilder.AddFilter("BMTP3", LogLevel.Information);
		});
	}
}