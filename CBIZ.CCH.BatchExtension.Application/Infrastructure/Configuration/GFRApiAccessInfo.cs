namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public class GfrApiAccessInfo
{
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string ApiAppId { get; init; }
    public required string DrawerId { get; init; }

}
