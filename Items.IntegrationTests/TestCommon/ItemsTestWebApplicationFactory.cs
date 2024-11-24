using System;
using Items.Domain;
using Items.WebAPI;
using Item.Common.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Items.IntegrationTests.TestCommon
{
    public class ItemsTestWebApplicationFactory : TestWebApplicationFactory<Program, ItemsContext>
    {
        public ItemsTestWebApplicationFactory(
            Action<ItemsContext> databaseSeeder = null,
            Action<IServiceCollection> configureServicesAction = null)
            : base(databaseSeeder, configureServicesAction)
        {
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
            });
        }
    }
}
