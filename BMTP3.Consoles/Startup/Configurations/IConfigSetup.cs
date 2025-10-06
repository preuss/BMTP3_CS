using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Startup.Configurations;
public interface IConfigSetup {
	void Configure(IConfigurationManager builder);
}
