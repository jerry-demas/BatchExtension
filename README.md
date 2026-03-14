# CBIZ.CCH.BatchExtension


### <u>Developer</u>
This process was created by Jerry DeMas 08/15/2025.
* VSCode: Version: 1.101.0 (user setup)
* .NET 9
* OS: Windows_NT x64 10.0.22631

### <u>Architecture</u>
This project uses\
[Vertical Slice](https://www.milanjovanovic.tech/blog/vertical-slice-architecture)\
[Options Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-9.0)\
[Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)\
[NLog](https://nlog-project.org/) 



### <u>Logging</u>
Logs are created in a log file and logging database with the created date. Location for this is configurable in appsetting.json:NLog


### <u>Locations</u>
|Type |Test|UAT|Production| 
|:----|:-----------|:-----------------|:----------|
|Databases  |DC5DSQL01\dev| DC5DSQL01/uat| TBD |
|Logging|DC5DSQL01\dev:BatchExtension|TBD|TBD|
|Project |TBD |TBD| TBD |

### <u>Running the application</u>
This is an api.

## <u>End points</u>

### addqueue
https://URLADDRESS/addqueue\
Pass this json as the body

```json
{
	"SubmittedBy":"first.last@cbiz.com",
	"ReturnType":"FedOnly",
	"Returns":[
		{	
			"FirmFlowId":["123456"],
			"ReturnId":"2025Z:98767:V1",
			"EngagementType":"1065 PARTNERSHIP TAX",
			"ClientName":"A Big Company",
			"ClientNumber":"98767",
			"OfficeLocation":"NATIONAL TAX OFFICE"
		}
	]
}
```
Result:\
```json
{
    "queueId": "5a90d208-0a11-f111-b39c-005056b22d70",
    "submittedBy": "first.last@cbiz.com"
}
```

### getbatchstatus/{GUID}
https://URLADDRESS/getbatchstatus/F534C40C-F2BB-F011-B398-005056B22D70\
Result:
```json
[
    {
        "queueId": "f534c40c-f2bb-f011-b398-005056b22d70",
        "queueStatus": "CompletedWithErrors",
        "batchItems": [
            {
                "firmFlowId": ",555555",
                "taxReturnId": "2024P:123456:V1",
                "itemStatus": "uploadErr",
                "statusDescription": "Error GFR upload"
            },
            {
                "firmFlowId": "111113",
                "taxReturnId": "2024P:123456:V1",
                "itemStatus": "uploadErr",
                "statusDescription": "Error GFR upload"
            }
        ]
    }
]
```

### getbatchstatus
https://URLADDRESS/getbatchstatus
Result:
```json
[
    {
        "queueId": "f534c40c-f2bb-f011-b398-005056b22d70",
        "queueStatus": "CompletedWithErrors",
        "batchItems": [
            {
                "firmFlowId": ",555555",
                "taxReturnId": "2024P:123456:V1",
                "itemStatus": "uploadErr",
                "statusDescription": "Error GFR upload"
            },
            {
                "firmFlowId": "111113",
                "taxReturnId": "2024P:123456:V1",
                "itemStatus": "uploadErr",
                "statusDescription": "Error GFR upload"
            }
        ]
    },
     {...},
     {...} .....
]


### getbatchextensiondata
https://URLADDRESS/getbatchextensiondata

Result:
```json
[
  {
    "returnType": "FedOnly",
    "submittedBy": "jerry.demas@cbiz.com",
    "queue": null,
    "id": "fb8b60ac-d7cf-f011-b399-005056b22d70",
    "queueIDGUID": "f35a2c94-d7cf-f011-b399-005056b22d70",
    "firmFlowId": "3237962",
    "taxReturnId": "2024P:271895:V1",
    "clientName": "HAYES, STANLEY & SIRENA",
    "clientNumber": "2303718",
    "officeLocation": "BLUE BELL",
    "engagementType": "1065 PARTNERSHIP TAX",
    "batchId": "98b0e257-4882-4422-ab8a-4761c3cab4a4",
    "batchItemGuid": "d02765db-4c1e-4097-8246-5e23423ec1cd",
    "batchItemStatus": "add",
    "statusDescription": "Added",
    "fileName": "2024US P271895 Extensions V1.pdf",
    "fileDownLoadedFromCCH": false,
    "fileUploadedToGFR": false,
    "gfrDocumentId": "",
    "message": "",
    "creationDate": "2025-12-02T18:35:48.697",
    "updatedDate": "2025-12-02T18:35:48.697"
  }, { ... }, ....
]

```




### requeueById
https://URLADDRESS/requeueById/F534C40C-F2BB-F011-B398-005056B22D70\
Result:
```json
{
    "queueId": "5a90d208-0a11-f111-b39c-005056b22d70",
    "submittedBy": "first.last@cbiz.com"
}
```


