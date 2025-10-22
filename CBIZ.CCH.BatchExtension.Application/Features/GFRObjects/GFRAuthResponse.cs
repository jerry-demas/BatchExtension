namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;



public record GFRAuthResponse(
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

);
