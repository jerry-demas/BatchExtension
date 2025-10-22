namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record BatchRecordStatus(string Code, string Description)
{
    public static readonly BatchRecordStatus Created = new("BACRE", "Created"); // Created status for db addition
    public static readonly BatchRecordStatus Initializing = new("BAINS", "Initializing");
    public static readonly BatchRecordStatus Scheduled = new("BASCH", "Scheduled");
    public static readonly BatchRecordStatus ReadyToRun = new("BARTR", "Ready-to-Run");
    public static readonly BatchRecordStatus InProcess = new("BAINP", "In-Process");
    public static readonly BatchRecordStatus Stopping = new("BASTG", "Stopping");
    public static readonly BatchRecordStatus Complete = new("BACMP", "Complete");
    public static readonly BatchRecordStatus Exception = new("BAEXC", "Exception");
    public static readonly BatchRecordStatus Terminated = new("BATRD", "Terminated");   

    public static IEnumerable<BatchRecordStatus> List() =>
            new[] { Created, Initializing, Scheduled, ReadyToRun,InProcess,Stopping,Complete, Exception, Terminated };

}

