namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record DatabaseOptions
{
    public required string DWConnectionString { get; init; }
}
