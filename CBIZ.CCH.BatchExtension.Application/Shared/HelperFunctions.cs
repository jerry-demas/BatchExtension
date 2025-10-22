namespace CBIZ.CCH.BatchExtension.Application;

internal static class HelperFunctions
{
    public static void CreateDirectory(string DirectoryName)
    {
        if (!Directory.Exists(DirectoryName))
            Directory.CreateDirectory(DirectoryName);
    }
}
