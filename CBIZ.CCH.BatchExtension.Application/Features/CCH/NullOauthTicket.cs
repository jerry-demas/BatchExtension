using CBIZ.CCH.BatchExtension.Application.Features.CCH.interfaces;

namespace CBIZ.CCH.BatchExtension.Application.Features.CCH;

public record NullOauthTicket : IOauthTicket
{
    public string access_token => String.Empty;
    public string refresh_token => String.Empty;
    public DateTime IssuedAt => DateTime.MinValue;
    public DateTime RefreshedAt => DateTime.MinValue;
}
