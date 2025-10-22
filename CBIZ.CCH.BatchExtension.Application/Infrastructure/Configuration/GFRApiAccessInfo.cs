namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public class GFRApiAccessInfo
{
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string X_TR_API_APP_ID { get; init; }
    public required string DrawerId { get; init; }

}
