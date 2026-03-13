# Collector
A web scraping platform for the AI era.

## Installation
`git clone https://github.com/Datasilk/Collector`
`git submodule init`
`git submodule update --init`
`cd Collector/Collector.App`
`npm install`

### History
I started this project in 2015 by building [Charlotte](https://github.com/Datasilk/Charlotte) along with a web UI in ASP.NET Core using C#. I've built this project from the ground up several times, and so now I am turning it into a set of tools enhanced by AI. 

## Setup Instructions
* In command prompt, run the following commands:

```bash
git clone https://github.com/Datasilk/Collector
git submodule init
git submodule update --init
cd Collector/Collector.Web.Client
npm install
```

* Afterwards, open `Collector.sln` in Visual Studio
* Follow instructions in (Collector.SQL/postgresql/README.md)[Collector.SQL] to deploy the database to PostgreSQL
* Update the connection string in `Collector.Web.Server/appsettings.json` to point to your PostgreSQL database
* Run the `Collector.Web.Server` project
* In your web browser, navigate to https://localhost:7783
* Click the Sign Up link and create your administrator account
* Log into your account and enjoy!

## Web application & web server projects
These projects compose the modern web experience for Collector, pairing an ASP.NET Core host with a React single-page application and a SQL Server backend.

### Collector.Web.Server
ASP.NET Core 8 host that boots the API and Auth assemblies, wires up SignalR hubs, background workers, large-file upload limits, and serves the React SPA plus static assets. It is the single entry point for running the web stack locally or in production.

### Collector.Web.Client
React + Vite client application that provides the user interface for dashboards, journal tooling, media workflows, and admin areas. It communicates exclusively through the API client classes in `src/api`, supports authenticated sessions, and consumes the SignalR hubs exposed by the server.

### Collector.API
Shared ASP.NET Core MVC assembly that contains the user, manager, admin, and public controllers. Each controller returns JSON `ApiResponse` objects, wraps repository calls in try/catch blocks, and enforces the platform conventions for public/admin routes.

### Collector.Auth
Authentication/authorization services and controllers, including JWT bearer handling, passwordless/one-time flows, salt management, policies, and integrations such as SendGrid for notifications. The Web Server loads this assembly so all auth endpoints live in the same host.

### Collector.Data
Dapper-based data access layer that encapsulates SQL operations behind repository interfaces. It references `Collector.Common` for shared models, exposes DI registration helpers, and is consumed by both the API and Auth projects.

### Collector.SQL
Database project that defines all SQL Server artifacts under `Collector.SQL/dbo` (tables, stored procedures, views, functions, indexes). These scripts mirror the structure expected by `Collector.Data` and are deployed manually to keep migrations explicit.

### Collector.Common
A common .NET library that contains all the common functionality of the Collector App so that you can build your own app to collect data from the web.