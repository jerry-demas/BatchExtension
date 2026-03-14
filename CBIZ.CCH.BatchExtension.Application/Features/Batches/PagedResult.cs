namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record PagedResult<T>(
    List<T> Data,
    int PageNumber,
    int PageSize,
    int TotalRecords,
    int TotalPages)
{
    public static PagedResult<T> Empty(int page, int pageSize) =>
        new PagedResult<T>(new List<T>(), page, pageSize, 0, 0);
}