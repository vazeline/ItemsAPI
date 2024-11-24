using Items.Data.EFCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Items.Domain.DomainEntities;

namespace Items.Domain.ModelConfiguration.Actions
{
    internal class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> entity)
        {
            entity.ToTable("Items");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).IsRequired().HasColumnName("Code");
            entity.Property(e => e.Value).IsUnicode(false).HasColumnName("Value");
        }
    }
}