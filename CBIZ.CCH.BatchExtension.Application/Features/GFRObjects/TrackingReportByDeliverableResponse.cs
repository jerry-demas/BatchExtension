namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record TrackingReportByDeliverableResponse(
    int TotalRecords,
    string PageNumber,
    int PageSize,
    int TotalPages,
    List<FirmFlowReportResponse> FirmFlowReportResponse,
    string GetFirmFlowException
)
{
    public static readonly TrackingReportByDeliverableResponse Empty =
        new TrackingReportByDeliverableResponse(
            TotalRecords: 0,
            PageNumber: string.Empty,
            PageSize : 0,
            TotalPages: 0,
            FirmFlowReportResponse: new List<FirmFlowReportResponse>(),
            GetFirmFlowException: string.Empty
        );

        public bool IsEmptyDeliverable => TotalRecords == 0;
};


