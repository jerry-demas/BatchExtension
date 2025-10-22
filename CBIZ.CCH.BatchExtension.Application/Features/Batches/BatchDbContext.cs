using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Maps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public class BatchDbContext(
    DbContextOptions<BatchDbContext> options,
    IOptions<DatabaseOptions> databaseOptions) : DbContext(options)
{

    private readonly DatabaseOptions _databaseOptions = databaseOptions.Value;


    public DbSet<BatchExtensionQueue> BatchQueue{ get; set; }

    public DbSet<BatchExtensionData> Batches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BatchExtensionMap());
        modelBuilder.ApplyConfiguration(new BatchExtensionQueueMap());

    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder.UseSqlServer(_databaseOptions.DWConnectionString);
}
