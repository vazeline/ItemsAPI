using Items.Domain.DomainEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Items.Domain.ModelConfiguration.Items
{
    internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> entity)
        {
            entity.ToTable("AuditLog");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.RequestUri).HasColumnName( "Request" );
            entity.Property(e => e.StatusCode).HasColumnName( "Response" );
            entity.Property(e => e.Method ).HasColumnName( "Method" );
            entity.Property( e => e.IpAddress ).HasColumnName( "IpAddress" );
            entity.Property( e => e.EventUtcDateTime ).HasColumnType( "datetime" ).HasColumnName( "EventUtcDateTime" );

        }
    }
}