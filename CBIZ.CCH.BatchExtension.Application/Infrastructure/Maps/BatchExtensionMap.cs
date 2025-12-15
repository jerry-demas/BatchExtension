using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;



namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Maps;

internal class BatchExtensionMap : IEntityTypeConfiguration<BatchExtensionData>
{
    public void Configure(EntityTypeBuilder<BatchExtensionData> builder)
    {

        builder.ToTable("BatchExtensionData").HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql();
        builder.Property(b => b.QueueIDGUID).IsRequired();
        builder.Property(b => b.FirmFlowId).IsRequired().HasMaxLength(50);
        builder.Property(b => b.TaxReturnId).HasMaxLength(50);    
        builder.Property(b => b.ClientName).HasMaxLength(100);    
        builder.Property(b => b.ClientNumber).HasMaxLength(100);    
        builder.Property(b => b.OfficeLocation).HasMaxLength(100); 
        builder.Property(b => b.EngagementType).HasMaxLength(50);    
        builder.Property(b => b.BatchItemStatus).HasMaxLength(50);
        builder.Property(b => b.StatusDescription).HasMaxLength(100);
        builder.Property(b => b.FileName).HasMaxLength(100);
        builder.Property(b => b.GfrDocumentId).HasMaxLength(50);
    }
}
