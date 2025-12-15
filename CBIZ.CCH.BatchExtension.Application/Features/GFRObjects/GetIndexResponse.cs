namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record GetIndexResponse(string IndexId, string IndexName)
{
    
    public static List<IndexItem> BuildFromRequest(
        List<GetIndexResponse> indexes,         
        GfrDocument document)
    {
        var valueMap = new Dictionary<string, string>
        {
            ["Client Name"] = document.clientName ?? string.Empty,
            ["Client Number"] = document.clientNumber ?? string.Empty,
            ["File Section"] = document.engagementType ?? string.Empty,            
            ["Document Type"] = document.documentType ?? string.Empty,
            //["Description"] = record.Description ?? string.Empty,
            ["Year"] =  document.returnId.TaxReturnYear(),
            //["Period End"] = record.PeriodEnd ?? string.Empty,
            ["Document Date"] = DateTime.Now.ToString("MM/dd/yyyy")
        };

        return [.. indexes
            .Where(index => valueMap.ContainsKey(index.IndexName))
            .Select(index => new IndexItem(index.IndexId, valueMap[index.IndexName]))];
    }


};