namespace CBIZ.CCH.BatchExtension.Application.Shared;

internal static class StringExtensions
{
    public static string TimeStamp(this string value, TimeProvider timeProvider) => $"{value} - {timeProvider.GetUtcNow().ToString(format: "yyyy-MM-dd HH:mm:ss")}";
    public static string TimeStamp(this string value) => TimeStamp(value, TimeProvider.System);

    public static string GuidString(this Guid value) => value.ToString();
    
}