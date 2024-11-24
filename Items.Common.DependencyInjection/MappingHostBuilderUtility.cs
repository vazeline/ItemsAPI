using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Items.Common.DependencyInjection
{
    public class MappingHostBuilderUtility
    {
        public static void ValidateMappings(IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILogger logger)
        {
            logger.LogDebug("Validating mappings...");

            using (var serviceScope = serviceProvider.CreateScope())
            {
                try
                {
                    var mapper = serviceScope.ServiceProvider.GetService<IMapper>();
                    mapper.ConfigurationProvider.AssertConfigurationIsValid();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Error in mapping configuration");
                    throw;
                }
            }
        }
    }
}