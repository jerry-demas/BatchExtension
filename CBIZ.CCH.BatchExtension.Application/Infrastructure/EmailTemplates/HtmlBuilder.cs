using System.Globalization;
using System.Text;
using System.Text.Json;


namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.EmailTemplates;

public static class HtmlBuilder
{
    const string BodyStyle = @"<style>
            body{font-family:Arial, sans-serif;}
            table{border-collapse:collapse;width:100%; border: 1px solid black;}
            th,td{border:1px solid #080707ff;padding:8px;text-align:left;}
            th{background-color:#f4f4f4;}
            </style>";

    public static string Header() => $"<head><meta charset='utf-8'>{BodyStyle}</head>";
    public static string TextValueColorRed(string value) => $"<p style='color: red;'>{value}</p>";
    public static string TextValueColorGreen(string value) => $"<p style='color: green;'>{value}</p>";
    public static string TableHeader(List<string> headers)
    {
        if (headers == null || headers.Count == 0)
            return "<thead></thead>";

        var headerCells = string.Join("", headers.Select(h => $"<th>{System.Net.WebUtility.HtmlEncode(h)}</th>"));
        return $"<thead><tr>{headerCells}</tr></thead>";
    }
    public static string TableRow(List<List<string>> rows)
    {
        var sb = new StringBuilder();

        if (rows == null || rows.Count == 0)
            return "<tr></tr>";

        foreach (var row in rows)
        {
            
            sb.Append("<tr>");
            foreach (var cell in row)
            {
                sb.Append(TableCell(cell));
            }
            sb.Append("</tr>");
        }

        return sb.ToString();
    }
    public static (List<string> headers, List<List<string>> dataRows) GetTableData(JsonElement items)
    {

        var headers = items
            .EnumerateArray()
            .SelectMany(obj => obj.EnumerateObject().Select(p => p.Name))
            .Distinct()
            .ToList();
        
        var rows = new List<List<string>>();
        foreach (var element in items.EnumerateArray())
        {
            var row = new List<string>();

            foreach (var header in headers)
            {
                if (element.TryGetProperty(header, out var value))
                {
                    row.Add(ExtractJsonElementValue(value));
                }
                else
                {
                    row.Add(string.Empty);
                }
            }

            rows.Add(row);
        }

        return (headers, rows);

       
    }

    private static string ExtractJsonElementValue(JsonElement value)
    {    
        return value.ValueKind switch
            {
                JsonValueKind.Array =>
                    string.Join(",",
                        value.EnumerateArray()
                            .Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
                            .Where(v => v != null)
                    ),
                JsonValueKind.True  => value.GetBoolean().ToString(),
                JsonValueKind.False => value.GetBoolean().ToString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.String => ParseStringValue(value),
                JsonValueKind.Null => string.Empty,
                _ => value.ToString()
            };
    }

    private static string ParseStringValue(JsonElement value)
    {
        var str = value.GetString();

        if (str is null)
            return string.Empty;

        return DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
            : str;
    }
    private static string TableCell(string cellValue) => $"<td>{cellValue}</td>";

}
