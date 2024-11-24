using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Utility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Item.Common.Testing
{
    public class TestWebApplicationFactory<TWebApiProgram, TDbContext> : WebApplicationFactory<TWebApiProgram>
        where TWebApiProgram : class
        where TDbContext : DbContext
    {
        private readonly Action<TDbContext> testDatabaseSeeder;
        private readonly Action<IServiceCollection> configureServicesAction;

        public TestWebApplicationFactory(
            Action<TDbContext> databaseSeeder = null,
            Action<IServiceCollection> configureServicesAction = null)
        {
            this.testDatabaseSeeder = databaseSeeder;
            this.configureServicesAction = configureServicesAction;

            // this "magic" enables all console output to be captured from a unit test after app.Run() is called (the logging framework will be logging to the console)
            // without it, console entries only appear in the test window's "standard output" section from before app.Run() is called
            this.Server.PreserveExecutionContext = true;
        }

        protected virtual List<Type> AdditionalDbContextTypesToReplaceWithSqlite { get; }

        /// <summary>
        /// Gets a DbContext in a new service scope, and executes the given action using it.
        /// </summary>
        public void ExecuteScopedContextAction(Action<TDbContext> contextAction)
        {
            using (var serviceScope = this.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<TDbContext>();

                ServiceUtility.UseServiceProviderForDomainEntityServiceAccessDuringAction(
                    serviceScope.ServiceProvider,
                    () => contextAction(context));
            }
        }

        /// <summary>
        /// Gets a DbContext in a new service scope, and executes the given action using it.
        /// </summary>
        public void ExecuteScopedContextAction(Action<TDbContext, IServiceProvider> contextAction)
        {
            using (var serviceScope = this.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<TDbContext>();

                ServiceUtility.UseServiceProviderForDomainEntityServiceAccessDuringAction(
                    serviceScope.ServiceProvider,
                    () => contextAction(context, serviceScope.ServiceProvider));
            }
        }

        /// <summary>
        /// Gets a DbContext in a new service scope, and executes the given async action using it.
        /// </summary>
        public async Task ExecuteScopedContextActionAsync(Func<TDbContext, Task> contextActionAsync)
        {
            using (var serviceScope = this.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<TDbContext>();

                await ServiceUtility.UseServiceProviderForDomainEntityServiceAccessDuringActionAsync(
                    serviceScope.ServiceProvider,
                    async () => await contextActionAsync(context));
            }
        }

        /// <summary>
        /// Gets a DbContext in a new service scope, and executes the given async action using it.
        /// </summary>
        public async Task ExecuteScopedContextActionAsync(Func<TDbContext, IServiceProvider, Task> contextActionAsync)
        {
            using (var serviceScope = this.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<TDbContext>();

                await ServiceUtility.UseServiceProviderForDomainEntityServiceAccessDuringActionAsync(
                    serviceScope.ServiceProvider,
                    async () => await contextActionAsync(context, serviceScope.ServiceProvider));
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter, TestWebApplicationFactoryStartupFilter>();

                // swap out the concrete database context for a SQLite in-memory one
                DataHostBuilderUtility.ReplaceDbContextInServicesWithSqlite<TDbContext>(services);

                if (this.AdditionalDbContextTypesToReplaceWithSqlite != null)
                {
                    foreach (var additionalDbContextTypesToReplaceWithSqlite in this.AdditionalDbContextTypesToReplaceWithSqlite)
                    {
                        DataHostBuilderUtility.ReplaceDbContextInServicesWithSqlite(services, additionalDbContextTypesToReplaceWithSqlite);
                    }
                }

                if (this.testDatabaseSeeder != null)
                {
                    services.AddSingleton(typeof(IDatabaseSeeder), new TestWebApplicationFactoryDatabaseSeeder<TDbContext>(this.testDatabaseSeeder));
                }

                this.configureServicesAction?.Invoke(services);
            });
        }
    }

    public class TestWebApplicationFactoryDatabaseSeeder<TDbContext> : IDatabaseSeeder
        where TDbContext : DbContext
    {
        private readonly Action<TDbContext> seeder;

        public TestWebApplicationFactoryDatabaseSeeder(Action<TDbContext> seeder)
        {
            this.seeder = seeder;
        }

        public void Seed(DbContext context)
        {
            SqliteUtility.ExecuteContextActionAndCatchForeignKeyViolations(
                context,
                context =>
                {
                    this.seeder(context as TDbContext);
                    context.SaveChanges();
                });
        }
    }

    public class TestWebApplicationFactoryStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<FakeRemoteIpAddressMiddleware>();
                next(app);
            };
        }
    }

    public class FakeRemoteIpAddressMiddleware
    {
        private readonly RequestDelegate next;

        public FakeRemoteIpAddressMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("12.34.56.78");

            await this.next(httpContext);
        }
    }
}
