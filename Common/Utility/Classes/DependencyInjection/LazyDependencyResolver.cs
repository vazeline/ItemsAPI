using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Utility.Classes.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Utility.Classes.DependencyInjection
{
    public class LazyDependencyResolver : ILazyDependencyResolver
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<Type, object> lazyServices = new ConcurrentDictionary<Type, object>();

        public LazyDependencyResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TService Get<TService>()
            where TService : class
        {
            var lazyService = (Lazy<TService>)this.lazyServices.GetOrAdd(
                key: typeof(TService),
                value: new Lazy<TService>(() =>
                {
                    var service = this.serviceProvider.GetService<TService>();

                    if (service == null)
                    {
                        throw new InvalidOperationException($"Failed to resolve service of type '{typeof(TService).Name}' - is it registered?");
                    }

                    return service;
                }));

            return lazyService.Value;
        }

        public TService Get<TService>(Func<TService> valueFactory)
            where TService : class
        {
            var lazyService = (Lazy<TService>)this.lazyServices.GetOrAdd(
                key: typeof(TService),
                value: new Lazy<TService>(valueFactory));

            return lazyService.Value;
        }
    }
}
