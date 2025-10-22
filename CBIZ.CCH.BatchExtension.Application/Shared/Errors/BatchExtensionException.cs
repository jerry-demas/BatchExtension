namespace CBIZ.CCH.BatchExtension.Application.Shared.Errors;

public class BatchExtensionException : Exception
{
    public BatchExtensionException() { }
    public BatchExtensionException(Exception ex) { }
    public BatchExtensionException(string message) : base(message) { }
    public BatchExtensionException(string message, Exception inner) : base(message, inner) { }
}
