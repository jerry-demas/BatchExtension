use BatchExtension



IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.BatchExtensionQueue') AND type = 'U')
BEGIN
CREATE TABLE BatchExtensionQueue(
	QueueId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),	
	QueueRequest NVARCHAR(MAX) NULL,
	QueueStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending',
	BatchStatus NVARCHAR(50) NOT NULL DEFAULT 'Created',
	ReturnType NVARCHAR(50) NOT NULL,
	SubmittedBy VARCHAR(50) NOT NULL,
	SubmittedDate DATETIME NOT NULL DEFAULT GETDATE(),	
)
END


IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.BatchExtensionData') AND type = 'U')
BEGIN
CREATE TABLE BatchExtensionData(	
	Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
	QueueIDGUID UNIQUEIDENTIFIER,
	FirmFlowID VARCHAR(50),
	TaxReturnID VARCHAR(50),   
	ClientName VARCHAR(100),
	ClientNumber VARCHAR(100),
	OfficeLocation VARCHAR(100),
	BatchID UNIQUEIDENTIFIER,
	BatchItemGUID UNIQUEIDENTIFIER,
	BatchItemStatus VARCHAR(50),
	StatusDescription VARCHAR(100),
	FileName VARCHAR(100),
	FileDownLoadedFromCCH bit,
	FileUploadedToGFR bit,
	CreationDate DATETIME NOT NULL DEFAULT GETDATE(),
	UpdatedDate DATETIME,
	EngagementType VARCHAR(50) NOT NULL,
	GFRDocumentId VARCHAR(50),
	Message VARCHAR(max)
)
END














