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
"SubmittedBy": "FirstName.LastName@cbiz.com",
    "ReturnType" : "FedOnly", // or "FedState
    "Returns" : 
        [
            {
                "FirmFlowId" :  ["111113",",555555"],
                "ReturnId": "2024P:123456:V1"
            }

        ]
}
```
Result:\
"QueueId:f534c40c-f2bb-f011-b398-005056b22d70"

### getbatchstatus
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

