using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;


namespace Items.Common.WebAPI.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMockHttpContextAccessor(this IServiceCollection services)
        {
            services.AddScoped(typeof(IHttpContextAccessor), _ =>
            {
                var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
                return mockHttpContextAccessor.Object;
            });
        }
    }
}
