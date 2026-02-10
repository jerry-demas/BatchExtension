IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.BatchExtensionDeliverableData') AND type = 'U')
BEGIN
CREATE TABLE BatchExtensionDeliverableData(	
	[Id] [int] NOT NULL,
	[Jurisdiction] VARCHAR(100) NOT NULL,
	[ReturnForm] VARCHAR(100) NOT NULL,
	[Deliverable] VARCHAR(200) NOT NULL,
	[ExtensionDate] Date NOT NULL
)
END