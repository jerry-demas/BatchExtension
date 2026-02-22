namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record GetIndexResponse(string IndexId, string IndexName)
{
    
    public static List<IndexItem> BuildFromRequest(
        List<GetIndexResponse> indexes,         
        GfrDocument document)
    {
        var valueMap = new Dictionary<string, string>
        {
            ["Client Name"] = document.ClientName ?? string.Empty,
            ["Client Number"] = document.ClientNumber ?? string.Empty,
            ["File Section"] = document.EngagementType ?? string.Empty,            
            ["Document Type"] = document.DocumentType ?? string.Empty,            
            ["Year"] =  document.ReturnId.TaxReturnYear(),            
            ["Document Date"] = DateTime.Now.ToString("MM/dd/yyyy"),
            ["Description"] = document.Description
        };        
        return [.. indexes
            .Where(index => valueMap.ContainsKey(index.IndexName))
            .Select(index => new IndexItem(index.IndexId, valueMap[index.IndexName]))];
    }

};