using System.Text.RegularExpressions;

namespace CBIZ.CCH.BatchExtension.Application.Shared;

internal static partial class RegexReplace
{
    //[GeneratedRegex(@"^(\d{4})US\s+P(\d+).*?V(\d+)(?:\.pdf)?$", RegexOptions.Compiled)]
    [GeneratedRegex(@"^(\d{4})US\s+P([A-Za-z]+-\d+|\d+).*?V(\d+)(?:\.pdf)?$", RegexOptions.Compiled)]
    public static partial Regex FileNameToReturnID();
    
}
