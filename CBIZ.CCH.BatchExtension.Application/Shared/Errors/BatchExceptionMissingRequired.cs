namespace CBIZ.CCH.BatchExtension.Application.Shared.Errors;

public class BatchExceptionMissingRequired : Exception
{
    public BatchExceptionMissingRequired(string requiredValues) : base($"Required values missing: {requiredValues}") { }
}
