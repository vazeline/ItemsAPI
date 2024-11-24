using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;

namespace Common.Utility.Classes.Lazy
{
    public class LazyWithTimeout<T>
    {
        private static readonly MemoryCache LazyWithTimeoutCache = new MemoryCache(
            "LazyWithTimeoutCache",
            new NameValueCollection(1)
            {
                { "pollingInterval", "00:00:01" }
            });

        private readonly string key;
        private readonly Func<T> valueFactory;
        private readonly TimeSpan cacheTimeout;

        public LazyWithTimeout(Func<T> valueFactory, TimeSpan cacheTimeout)
        {
            this.key = Guid.NewGuid().ToString();
            this.valueFactory = valueFactory;
            this.cacheTimeout = cacheTimeout;

            this.AddToCache();
        }

        public T Value
        {
            get
            {
                if (LazyWithTimeoutCache.Get(this.key) is not Lazy<T> value)
                {
                    value = this.AddToCache();
                }

                try
                {
                    return value.Value;
                }
                catch
                {
                    // Handle exception if valueFactory throws.
                    LazyWithTimeoutCache.Remove(this.key);
                    throw;
                }
            }
        }

        private Lazy<T> AddToCache()
        {
            var lazy = new Lazy<T>(this.valueFactory);

            LazyWithTimeoutCache.Add(
                this.key,
                lazy,
                new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(this.cacheTimeout)
                });

            return lazy;
        }
    }
}
