	use BatchExtension
	
	Insert into BatchExtensionData (
		QueueIDGUID, 
		FirmFlowID, 
		TaxReturnID, 
		ClientName,
		ClientNumber, 
		OfficeLocation, 
		BatchID, 
		BatchItemGUID, 
		BatchItemStatus, 
		StatusDescription, 
		FileName,
		FileDownLoadedFromCCH,
		FileUploadedToGFR,
		CreationDate,
		UpdatedDate, 
		EngagementType, 
		GFRDocumentId,
		Message,
		Pic)
	select 
		q.QueueId,
		'',
		'',
		'',
		'',
		'',
		'00000000-0000-0000-0000-000000000000',
		'00000000-0000-0000-0000-000000000000',
		'inserted',
		'Inserted',
		'',
		0,
		0,
		GetDate(),
		GetDate(),
		'',
		'',
		'',
		''
	from BatchExtensionQueue q left join BatchExtensionData d on q.QueueId = d.QueueIDGUID
	where d.QueueIDGUID is null
	order by q.SubmittedDate desc