using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Items.Data.EFCore.Utility
{
    public class DataHostBuilderUtility
    {
        public static void UpgradeDatabase<TDbContext>(
            IServiceProvider serviceProvider,
            ILogger logger)
            where TDbContext : DbContext
        {
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<TDbContext>();

                logger.LogDebug("Ensuring database exists...");
                context.Database.EnsureCreated();

                // we only use in-memory or sqlite provider for testing - migrations not needed
                if (!context.Database.IsInMemory() && !context.Database.IsSqlite())
                {
                    logger.LogDebug("Migrating database if required...");
                    context.Database.Migrate();
                }

                // execute any registered database seeders
                var databaseSeeders = serviceScope.ServiceProvider.GetServices<IDatabaseSeeder>();

                if (databaseSeeders.Any())
                {
                    logger.LogDebug("Seeding database...");

                    foreach (var databaseSeeder in databaseSeeders)
                    {
                        databaseSeeder.Seed(context);
                    }
                }
            }
        }

        public static void ReplaceDbContextInServicesWithSqlite(IServiceCollection services, Type dbContextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
            {
                throw new ArgumentException($"Type {dbContextType.Name} does not inherit from {nameof(DbContext)}", nameof(dbContextType));
            }

            var replaceDbContextInServicesWithSqliteMethod = typeof(DataHostBuilderUtility)
                .GetMethods()
                .Where(x => x.Name == nameof(ReplaceDbContextInServicesWithSqlite) && x.IsGenericMethod)
                .Single();

            var typedReplaceDbContextInServicesWithSqliteMethod = replaceDbContextInServicesWithSqliteMethod.MakeGenericMethod(dbContextType);

            typedReplaceDbContextInServicesWithSqliteMethod.Invoke(null, new object[] { services });
        }

        public static void ReplaceDbContextInServicesWithSqlite<TDbContextToReplace>(IServiceCollection services)
            where TDbContextToReplace : DbContext
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<TDbContextToReplace>)));
            services.Remove(services.Single(d => d.ServiceType == typeof(TDbContextToReplace)));
            services.AddDbContextForSqliteInMemory<TDbContextToReplace>();
        }
    }
}
