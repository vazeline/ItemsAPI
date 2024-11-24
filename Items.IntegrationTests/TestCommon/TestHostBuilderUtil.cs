using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Items.Common.DependencyInjection;
using Items.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Common.Utility;
using Items.Data.EFCore.Utility;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Common.WebAPI.DependencyInjection;

namespace Items.IntegrationTests.TestCommon
{
    public class TestHostBuilderUtil
    {
        public static async Task RunTestHostAsync(Func<IServiceProvider, Task> testWorkerStartupFunc)
        {
            await RunTestHostAsync(null, null, testWorkerStartupFunc);
        }

        public static async Task RunTestHostAsync(Action<ItemsContext> databaseSeeder, Func<IServiceProvider, Task> testWorkerStartupFunc)
        {
            await RunTestHostAsync(databaseSeeder, null, testWorkerStartupFunc);
        }

        public static async Task RunTestHostAsync(
            Action<ItemsContext> databaseSeeder,
            Action<IServiceCollection> configureServicesAction,
            Func<IServiceProvider, Task> testWorkerStartupFunc)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentUtility.CurrentEnvironmentName);

            var builder = Host.CreateDefaultBuilder();

            HostBuilderUtil.ConfigureHostBuilderCommon(builder);

            builder.ConfigureServices((hostContext, services) =>
            {
                HostBuilderUtil.ConfigureServicesCommon(
                    services,
                    hostContext.Configuration);

                // swap out the concrete database context for a SQLite in-memory one
                DataHostBuilderUtility.ReplaceDbContextInServicesWithSqlite<ItemsContext>(services);

                if (databaseSeeder != null)
                {
                    services.AddSingleton(typeof(IDatabaseSeeder), new TestHostDatabaseSeeder(databaseSeeder));
                }

                services.AddMockHttpContextAccessor();

                services.AddHostedService(serviceProvider => TestHostWorker.Create(serviceProvider, testWorkerStartupFunc));

                configureServicesAction?.Invoke(services);
            });

            var app = builder.Build();

            HostBuilderUtil.ConfigureAppCommon<ItemsContext>(
                app,
                useLogicanTemplatingEngine: false);

            var logger = app.Services.GetRequiredService<ILogger<TestHostBuilderUtil>>();
            logger.LogDebug("Startup complete");

            await app.RunAsync();
        }
    }

    public class TestHostDatabaseSeeder : IDatabaseSeeder
    {
        private readonly Action<ItemsContext> seeder;

        public TestHostDatabaseSeeder(Action<ItemsContext> seeder)
        {
            this.seeder = seeder;
        }

        public void Seed(DbContext context)
        {
            SqliteUtility.ExecuteContextActionAndCatchForeignKeyViolations(
                context,
                context =>
                {
                    this.seeder(context as ItemsContext);
                    context.SaveChanges();
                });
        }
    }

    public class TestHostWorker : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IServiceProvider, Task> startupFunc;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        private TestHostWorker(IServiceProvider serviceProvider, Func<IServiceProvider, Task> startupFunc)
        {
            this.serviceProvider = serviceProvider;
            this.startupFunc = startupFunc;
            this.hostApplicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ServiceUtility.UseServiceProviderForDomainEntityServiceAccessDuringActionAsync(
                this.serviceProvider,
                async () => await this.startupFunc(this.serviceProvider));

            this.hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        internal static TestHostWorker Create(IServiceProvider serviceProvider, Func<IServiceProvider, Task> startupFunc)
        {
            return new TestHostWorker(serviceProvider, startupFunc);
        }
    }
}
