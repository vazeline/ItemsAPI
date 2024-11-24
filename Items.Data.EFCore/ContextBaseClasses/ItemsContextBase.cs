using Items.Data.EFCore.ContextBaseClasses.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Items.Data.EFCore.ContextBaseClasses
{
    public abstract class ItemsContextBase : DbContext
    {
        private readonly IConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;

        protected ItemsContextBase(IConfiguration configuration, ILoggerFactory loggerFactory)
            : this(configuration, loggerFactory, null)
        {
        }

        protected ItemsContextBase(IConfiguration configuration, ILoggerFactory loggerFactory, DbContextOptions options)
            : base(options)
        {
            this.configuration = configuration;
            this.loggerFactory = loggerFactory;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (this.configuration?.GetValue<bool?>("QueryLoggingMetrics:Enabled") == true)
            {
                optionsBuilder.AddInterceptors(new QueryMetricsLoggingInterceptor(
                    loggerFactory: this.loggerFactory,
                    minSizeKb: this.configuration.GetValue<int?>("QueryLoggingMetrics:MinSizeKb") ?? 0));
            }

            base.OnConfiguring(optionsBuilder);
        }
    }
}
