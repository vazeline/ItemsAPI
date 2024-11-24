using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Items.Domain.DomainEntities;
using Items.Domain.ModelConfiguration.Actions;
using Items.Domain.ModelConfiguration.Items;
using Items.Data.EFCore.ContextBaseClasses;
using Items.Data.EFCore.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Items.Domain
{
    public class ItemsContext : ItemsContextBase
    {
        public ItemsContext()
            : base(
                null,
                null,
                new DbContextOptionsBuilder<ItemsContext>().UseSqlServer().Options)
        {
        }

        public ItemsContext(
            DbContextOptions<ItemsContext> options,
            ILogger<ItemsContext> logger,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IHttpContextAccessor httpContextAccessor = null)
            : base(
                configuration: configuration,
                loggerFactory: loggerFactory,
                options: options)
        {
        }

        public DbSet<Item> Items { get; set; }

        public DbSet<AuditLog> ItemAuditLogs { get; set; }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigurePrimaryKeys(modelBuilder);

            // these have been applied in a specific sequence, so that later ones that depend on earlier navigation properties will work
            // DO NOT ALTER
            modelBuilder.ApplyConfiguration(new ItemConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

            SetDefaultDecimalPrecision(modelBuilder);

            SqliteUtility.RenameCustomSchemaTables(this.Database, modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
        {
            var derivedTypes = modelBuilder.Model.GetEntityTypes()
                .Where(t => t.ClrType.BaseType == typeof(ItemsDomainEntityBase))
                .ToList();

            foreach (var entityType in derivedTypes)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasKey(nameof(ItemsDomainEntityBase.Id));
            }
        }

        private static void SetDefaultDecimalPrecision(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var decimalProperties = entityType
                    .GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                    .ToList();

                foreach (var property in decimalProperties)
                {
                    // skip if already explicitly set
                    if (property.GetColumnType()?.StartsWith("decimal") != true)
                    {
                        property.SetColumnType("decimal(18, 2)");
                    }
                }
            }
        }

        private bool WasEntityDeleted(object entity)
        {
            return this.WasEntityDeleted(this.Entry(entity));
        }
    }
}
