using CBIZ.CCH.BatchExtension.Application.Features.CCH.interfaces;

namespace CBIZ.CCH.BatchExtension.Application.Features.CCH;

public record OauthTicket(string access_token, string refresh_token, DateTime IssuedAt, DateTime RefreshedAt) : IOauthTicket;
