using System;

namespace Common.Utility.Classes.DependencyInjection.Interfaces
{
    public interface ILazyDependencyResolver
    {
        TService Get<TService>()
            where TService : class;

        TService Get<TService>(Func<TService> valueFactory)
            where TService : class;
    }
}
