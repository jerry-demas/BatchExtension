namespace CBIZ.CCH.BatchExtension.Application;

public record BatchExtensionDeliverableData
(

    int Id,
    string Jurisdiction,
    string ReturnForm,
    string Deliverable,
    DateTime ExtensionDate
)
{
    public static readonly BatchExtensionDeliverableData Empty =
            new BatchExtensionDeliverableData(
                Id: 0,
                Jurisdiction: string.Empty,
                ReturnForm : string.Empty,
                Deliverable: string.Empty,
                ExtensionDate: DateTime.MinValue
             );    
};