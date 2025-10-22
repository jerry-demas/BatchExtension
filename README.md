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
Logs are created in a log file with the created date. Location for this is configurable in appsetting.json:NLog


### <u>Locations</u>
|Type |Test Server |UAT server |Production Server | 
|:----|:-----------|:-----------------|:----------|
|Databases  |TBD| TBD| TBD |
|Project |TBD |TBD| TBD |

### <u>Running the application</u>
This is an api end point.
Pass this json as the body
```json
{
    "returnIds" : ["2023Z:123456:V2","2023G:789101:V2", ....]
}
```