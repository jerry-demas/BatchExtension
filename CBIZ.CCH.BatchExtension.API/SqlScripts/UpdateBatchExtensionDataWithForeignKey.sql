use BatchExtension

ALTER TABLE [dbo].[BatchExtensionData]
ADD CONSTRAINT FK_BatchExtensionData_Queue
FOREIGN KEY (QueueIDGUID)        
REFERENCES [dbo].[BatchExtensionQueue](QueueId)  
ON DELETE CASCADE;  

ALTER TABLE [dbo].[BatchExtensionData]
ALTER COLUMN QueueIDGUID UNIQUEIDENTIFIER NOT NULL;