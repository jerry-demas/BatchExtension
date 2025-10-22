namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record IndexValidationResult( bool IsValid, List<IndexValidationError> Errors, string ErrorMessage );


