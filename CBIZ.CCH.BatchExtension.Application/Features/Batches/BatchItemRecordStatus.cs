namespace CBIZ.CCH.BatchExtension.Application;

public record BatchItemRecordStatus(string Code, string Description)
{

    public static readonly BatchItemRecordStatus Unprocessed = new("BIUN", "Unprocessed"); // Created status for db addition
    public static readonly BatchItemRecordStatus InProcess = new("BIINP", "In-Process");
    public static readonly BatchItemRecordStatus Processed = new("BIPCD", "Processed");
    public static readonly BatchItemRecordStatus Stopping = new("BISTG", "Stopping");
    public static readonly BatchItemRecordStatus Stopped = new("BISTD", "Stopped");
    public static readonly BatchItemRecordStatus Complete = new("BICMP", "Complete");
    public static readonly BatchItemRecordStatus Exception = new("BIERR", "Exception");
    public static readonly BatchItemRecordStatus Terminated = new("BITRD", "Terminated");
    public static readonly BatchItemRecordStatus Canceled = new("BICND", "Canceled");  
    public static readonly BatchItemRecordStatus Unknown = new("BIUNK", "Unknown");   

    public static IEnumerable<BatchItemRecordStatus> List() =>
            new[] { Unprocessed, InProcess, Processed, Stopping, Stopped, Complete, Exception, Terminated, Canceled, Unknown };



}
