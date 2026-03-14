use BatchExtension

IF COL_LENGTH('dbo.BatchExtensionData', 'Pic') IS NULL
BEGIN
    ALTER TABLE [dbo].[BatchExtensionData]
    ADD Pic VARCHAR(100) NULL        
END