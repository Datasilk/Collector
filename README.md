# Collector
A web scraping platform for the AI era.

### History
I started this project in 2015 by building [Charlotte](https://github.com/Datasilk/Charlotte) along with a web UI in ASP.NET Core using C#. I've built this project from the ground up several times, and so now I am turning it into a set of tools enhanced by AI. 

## Prerequisites
Before setting up Collector, ensure you have the following installed:

* **Visual Studio 2022** (or later) with the following workloads:
  * ASP.NET and web development
  * .NET desktop development
* **.NET 8 SDK** (or later)
* **Node.js** (v18 or later) and npm
* **PostgreSQL** (v14 or later)
* **Ollama** - Local AI model runtime for AI-powered features
  * Download from [ollama.ai](https://ollama.ai)

## Setup Instructions
* In command prompt, execute the following commands:

```bash
git clone https://github.com/Datasilk/Collector
```

* Then, open "x64 Native Tools Command Prompt for VS" (run as administrator) and navigate to the newly cloned Collector directory
* Run the following command:

```bash
setup
```

* Afterwards, open `Collector.sln` in Visual Studio
* Update the connection string in `Collector.Web.Server/appsettings.json` to point to your PostgreSQL database
* Run the `Collector.Web.Server` project
* In your web browser, navigate to https://localhost:7783
* Click the Sign Up link and create your administrator account
* Log into your account and enjoy!

## Web application & web server projects
These projects compose the modern web experience for Collector, pairing an ASP.NET Core web server host with a React single-page application and a PostgreSQL Server backend.

### Collector.API
Shared ASP.NET Core MVC assembly that contains the user, manager, admin, and public controllers. Each controller returns JSON `ApiResponse` objects, wraps repository calls in try/catch blocks, and enforces the platform conventions for public/admin routes.

### Collector.Web.Server
ASP.NET Core 8 host that boots the API and Auth assemblies, wires up SignalR hubs, background workers, large-file upload limits, and serves the React SPA plus static assets. It is the single entry point for running the web stack locally or in production.

### Collector.Web.Client
React + Vite client application that provides the user interface for journals, media workflows, real-time AI chat agents, and admin dashboard. It communicates exclusively through the API client classes in `src/api`, supports authenticated sessions, and consumes the SignalR hubs & workers exposed by the server.

### Collector.Auth
Authentication/authorization services and controllers, including JWT bearer handling, passwordless/one-time flows, salt management, policies, and integrations such as SendGrid for notifications. The Web Server loads this assembly so all auth endpoints live in the same host.

### Collector.Data
Dapper-based data access layer that encapsulates SQL operations behind repository interfaces. It references `Collector.Common` for shared models, exposes DI registration helpers, and is consumed by both the API and Auth projects.

### Collector.SQL
Database project that defines the PostgreSQL database scheme under `Collector.SQL/postgres` (tables, sequences, indexes, & functions). These scripts mirror the structure expected by `Collector.Data` and are deployed manually to keep migrations explicit.

### Collector.Common
A common .NET library that contains all the common functionality of the Collector App so that you can build your own app to collect data from the web.