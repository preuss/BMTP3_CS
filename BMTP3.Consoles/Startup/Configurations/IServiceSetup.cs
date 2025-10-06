using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Startup.Configurations;
public interface IServiceSetup {
	void Configure(IServiceCollection services, IConfiguration configuration);
}