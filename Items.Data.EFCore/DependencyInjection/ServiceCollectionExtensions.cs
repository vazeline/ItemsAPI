using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Items.Data.EFCore.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDbContextForSqlServer<TDbContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName,
            IModel useCompiledModel = null)
            where TDbContext : DbContext
        {
            services.AddDbContext<TDbContext>(options =>
            {
                SuppressDbContextWarnings(options);

                if (configuration.GetValue<bool?>("AppSettings:EnableDbContextSensitiveDataLogging") == true)
                {
                    options.EnableSensitiveDataLogging();
                }

                options.UseSqlServer(
                    configuration.GetConnectionString(connectionStringName),
                    options =>
                    {
                        var sqlTimeoutSettings = configuration.GetValue<int?>("AppSettings:SqlTimeoutSeconds");

                        if (sqlTimeoutSettings != null)
                        {
                            options.CommandTimeout(sqlTimeoutSettings.Value);
                        }
                    });

                if (useCompiledModel != null)
                {
                    options.UseModel(useCompiledModel);
                }
            });
        }

        public static void AddDbContextForSqliteInMemory<TDbContext>(
            this IServiceCollection services,
            IModel useCompiledModel = null)
            where TDbContext : DbContext
        {
            // with SQLite provider, you have to create a connection and open it first
            // or else you'll get "table not found" errors the first time you try to query the context
            // undocumented "feature" obviously...
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder()
            {
                DataSource = $"{Guid.NewGuid()}.db",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
            }.ConnectionString);

            connection.Open();
            connection.EnableExtensions(true);

            services.AddDbContext<TDbContext>(options =>
            {
                SuppressDbContextWarnings(options);
                options.UseSqlite(connection);
                options.EnableSensitiveDataLogging();

                if (useCompiledModel != null)
                {
                    options.UseModel(useCompiledModel);
                }
            });
        }

        private static void SuppressDbContextWarnings(DbContextOptionsBuilder options)
        {
            options.ConfigureWarnings(w => w
                .Ignore(RelationalEventId.MultipleCollectionIncludeWarning)
                .Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)
                .Ignore(CoreEventId.NavigationBaseIncludeIgnored));
        }
    }
}
