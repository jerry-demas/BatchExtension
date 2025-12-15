namespace CBIZ.CCH.BatchExtension.Application;

internal static class HelperFunctions
{
    public static void CreateDirectory(string DirectoryName)
    {
        if (!Directory.Exists(DirectoryName))
            Directory.CreateDirectory(DirectoryName);
    }

    public static string TaxReturnClientNumber(this string taxReturn)
    {
        if (taxReturn == string.Empty) return string.Empty;
        string[] parts = taxReturn.Split(':');
        return parts[1].ToString();
    }

    public static string TaxReturnYear(this string taxReturn)
    {
        if (taxReturn == string.Empty) return string.Empty;
        return new string(taxReturn.TakeWhile(char.IsDigit).ToArray());
    }
 

}
