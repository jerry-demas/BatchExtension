namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record TrackingReportByDeliverableResponse(
    int TotalRecords,
    int PageNumber,
    int PageSize,
    int TotalPages,
    List<FirmFlowReportResponse> FirmFlowReportResponses,
    List<TrackingReportRoutingSummary> RoutingSummary,
    string GetFirmFlowException
)
{
    public static readonly TrackingReportByDeliverableResponse Empty =
        new TrackingReportByDeliverableResponse(
            TotalRecords: 0,
            PageNumber: 0,
            PageSize : 0,
            TotalPages: 0,
            FirmFlowReportResponses: new List<FirmFlowReportResponse>(),
            RoutingSummary: new List<TrackingReportRoutingSummary>(),
            GetFirmFlowException: string.Empty
        );

        public bool IsEmptyDeliverable => TotalRecords == 0;
};


