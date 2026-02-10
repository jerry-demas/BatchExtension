namespace CBIZ.CCH.BatchExtension.Application.Features.CCH.interfaces;

public interface IOauthTicket
{
    string access_token { get; }
    string refresh_token { get; }
    DateTime IssuedAt { get; }
    DateTime RefreshedAt { get; }
}
