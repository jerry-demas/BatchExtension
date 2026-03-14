use BatchExtension

IF COL_LENGTH('dbo.BatchExtensionData', 'RawMessage') IS NULL
BEGIN
    ALTER TABLE [dbo].[BatchExtensionData]
    ADD RawMessage VARCHAR(Max) NULL        
END 