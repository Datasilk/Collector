---
trigger: always_on
description: whenever I am working on an API
---

# Collector.API ASP.NET API class project

* all API controller must inherit from ApiController
* All API responses must return the Json object. e.g.: Json(new ApiResponse { });
* For admin functionality, use admin APIs, for authenticated user/manager functionality, use user APIs, and for public functionality, use public APIs
* All user/manager & public API controllers are found in Collector.API/Controllers folder and admin APIs are found in Collector.API/Controllers/Admin
* Use POST for APIs that need a model
* Use GET for APIs that have no parameters or a few simple parameters like integer IDs
* only use the message property in ApiResponse when returning error messages
* wrap all controller methods repository calls in try catch and if there is an exception, return an ApiResponse with the ex.Message as the ApiResponse.message

# Collector.DAL .NET Dapper class project
* All repositories require an interface
* enums belong in the Collector.DAL/Enums folder
* Don't forget to add new repositories to the Collector.DAL/Services/DapperStartupService.cs as a transient service

# Collector.SQL SQL Server project
* Do NOT create migration scripts
* all tables, stored procedures, functions, indexes, sequences, and views are located in Collector.SQL/dbo folder
* After you add/modify any scripts in the dbo folder, I will manually publish the changes to the local SQL Server

# Collector.Web.Client React web app project
* All API endpoints are accessed from JavaScript classes found in Collector.Web.Client/src/api. there are apis in the following sub-folders: account, admin, public, and user
* any component that utilizes an API must pass the useSession() context instance into the api class instance, for example:
```
const session = useSession();
const {add, getById, filter} = PresentationTemplates(session);
```

* make sure the API controller exists
* make sure the client-side API js file also exists