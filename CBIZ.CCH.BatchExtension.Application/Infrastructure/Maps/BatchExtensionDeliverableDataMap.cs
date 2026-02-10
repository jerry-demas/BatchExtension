using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Maps;

internal class BatchExtensionDeliverableDataMap : IEntityTypeConfiguration<BatchExtensionDeliverableData>
{
public void Configure(EntityTypeBuilder<BatchExtensionDeliverableData> builder)
    {
        builder.ToTable("BatchExtensionDeliverableData").HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql();
        builder.Property(b => b.Jurisdiction).HasMaxLength(100).IsRequired();;
        builder.Property(b => b.ReturnForm).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Deliverable).HasMaxLength(200).IsRequired();
        builder.Property(b => b.ExtensionDate).IsRequired();
    }
}
