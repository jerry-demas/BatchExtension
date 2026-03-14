using System.Text.Json;

namespace CBIZ.CCH.BatchExtension.Application;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
