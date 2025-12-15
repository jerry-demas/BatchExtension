namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;



public record GfrAuthResponse(
    string token,
    bool authSuccess,
    object authenticationResponse,
    string userName,
    string userId,
    string customerId,
    int idleSessionTimeoutMinutes,
    string loneStarFirmId,
    bool loneStarValidated,
    object errorCode

)
{
    public static readonly GfrAuthResponse Empty =
        new GfrAuthResponse(
            token: string.Empty,
            authSuccess: false,
            authenticationResponse: new object(),
            userName: string.Empty,
            userId: string.Empty,
            customerId: string.Empty,
            idleSessionTimeoutMinutes: 0,
            loneStarFirmId: string.Empty,
            loneStarValidated: false,
            errorCode: new object()
        );
    
    public bool IsEmptyToken => string.IsNullOrWhiteSpace(token);
};
