using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Maps;

internal class BatchExtensionQueueMap : IEntityTypeConfiguration<BatchExtensionQueue>
{
    public void Configure(EntityTypeBuilder<BatchExtensionQueue> builder)
    {
        builder.ToTable("BatchExtensionQueue").HasKey(b => b.QueueId);
        builder.Property(b => b.QueueId).HasDefaultValueSql();        
        builder.Property(b => b.QueueStatus).HasMaxLength(50).IsRequired();
        builder.Property(b => b.BatchStatus).HasMaxLength(50).IsRequired();
        builder.Property(b => b.ReturnType).HasMaxLength(50).IsRequired();
        builder.Property(b => b.SubmittedBy).HasMaxLength(50).IsRequired();
        builder.Property(b => b.SubmittedDate).HasDefaultValueSql();

        builder.HasMany(e => e.BatchExtensionData)
            .WithOne(d => d.Queue)
            .HasForeignKey(d => d.QueueIDGUID)
            .HasPrincipalKey(q => q.QueueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
