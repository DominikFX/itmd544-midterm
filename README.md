# Task Tracker API

A contract-first REST API for managing tasks, built with **ASP.NET Core Minimal API** (.NET 10) and **OpenAPI 3.1.0**. The hand-written OpenAPI specification serves as the single source of truth — request validation is driven by the spec, not hand-coded in handlers.

## Live URLs

| Resource | URL |
|----------|-----|
| **Swagger UI** | [https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/docs](https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/docs) |
| **Client App** | [https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/client](https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/client) |
| `/openapi.yaml` | [https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/openapi.yaml](https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/openapi.yaml) |
| `/openapi.json` | [https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/openapi.json](https://itmd544-midterm-atdsanc8azbyeha3.canadacentral-01.azurewebsites.net/openapi.json) |

## Setup & Run

### Prerequisites

- .NET 10 SDK
- Node.js

### Environment Variables

Copy `.env.example` and set your database connection string:

```bash
ConnectionStrings__DefaultConnection=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;User ID=<user>;Password=<pass>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

On Azure App Service, this is set via **Settings → Environment variables → Connection strings** (Name: `DefaultConnection`, Type: `SQLAzure`).

### Local Development

```bash
cd itmd544-midterm/task-tracker

export ConnectionStrings__DefaultConnection="<connection-string>"

dotnet restore
dotnet run
```

The API will be available at `http://localhost:5000`. The client app is at `http://localhost:5000/client`.

## Database Schema

The API uses Azure SQL Database with a single table:

### `Tasks` Table

| Column | SQL Type | Constraints |
|--------|----------|-------------|
| `Id` | `nvarchar(450)` | **Primary Key** (UUID string) |
| `Title` | `nvarchar(200)` | NOT NULL |
| `Assignee` | `nvarchar(100)` | NOT NULL |
| `Status` | `nvarchar(20)` | NOT NULL - `Pending`, `InProgress`, or `Done` |
| `Hours` | `int` | NOT NULL |
| `DueDate` | `nvarchar(10)` | NOT NULL - ISO 8601 date (`YYYY-MM-DD`) |

## Client Generation

The TypeScript client SDK was generated using [@hey-api/openapi-ts](https://heyapi.dev) from the OpenAPI specification.

### Generation Command

```bash
cd client
npm install
npx openapi-ts -i ../openapi.yaml -o ./generated -c @hey-api/client-fetch
```

### Build Command

```bash
npx esbuild app.ts --bundle --outfile=../task-tracker/wwwroot/app.js --format=esm
```

### Generated SDK → operationId Mapping

| SDK Method | operationId | HTTP |
|------------|-------------|------|
| `taskServiceList()` | `TaskService_list` | `GET /` |
| `taskServiceCreate({ body })` | `TaskService_create` | `POST /` |
| `taskServiceGet({ path: { id } })` | `TaskService_get` | `GET /{id}` |
| `taskServiceUpdate({ path: { id }, body })` | `TaskService_update` | `PATCH /{id}` |
| `taskServiceDelete({ path: { id } })` | `TaskService_delete` | `DELETE /{id}` |
| `taskServiceSummary()` | `TaskService_summary` | `GET /summary` |

## OperationId → Handler Mapping

| operationId | Method | Path | Handler | File |
|-------------|--------|------|---------|------|
| `TaskService_list` | GET | `/` | `TaskServiceList.Handle` | `Handlers/TaskServiceList.cs` |
| `TaskService_create` | POST | `/` | `TaskServiceCreate.Handle` | `Handlers/TaskServiceCreate.cs` |
| `TaskService_get` | GET | `/{id}` | `TaskServiceGet.Handle` | `Handlers/TaskServiceGet.cs` |
| `TaskService_update` | PATCH | `/{id}` | `TaskServiceUpdate.Handle` | `Handlers/TaskServiceUpdate.cs` |
| `TaskService_delete` | DELETE | `/{id}` | `TaskServiceDelete.Handle` | `Handlers/TaskServiceDelete.cs` |
| `TaskService_summary` | GET | `/summary` | `TaskServiceSummary.Handle` | `Handlers/TaskServiceSummary.cs` |

## Custom Operation: `/summary`

The `TaskService_summary` endpoint returns aggregated task statistics:
- **totalTasks** — count of all tasks
- **totalHours** — sum of estimated hours across all tasks
- **averageHours** — average hours per task
- **tasksByStatus** — breakdown of tasks grouped by status (Pending, InProgress, Done)

This goes beyond basic CRUD by providing a derived analytical view of the data.

## Deployment

This API deploys to Azure App Service (.NET 10) via GitHub Actions.

### Steps:
1. Create an Azure App Service (Linux, .NET 10)
2. Create an Azure SQL Database and add the connection string as an App Service connection string (`DefaultConnection`, type `SQLAzure`)
3. Set up GitHub Actions with Azure credentials (see `.github/workflows/`)
4. Push to `main` — GitHub Actions handles the rest

---

## Reflection

### Domain and Stack Choice

I chose the **Task Tracker** domain because it naturally supports the assignment's requirements: tasks have multiple meaningful fields (`title`, `assignee`, `status`, `hours`, `dueDate`), the `status` field maps cleanly to an enum (`Pending`, `InProgress`, `Done`), and the numeric `hours` field enables interesting aggregation for the custom `/summary` operation. The domain is practical and relatable - something any developer has interacted with.

For the technology stack, I selected C# with ASP.NET Core Minimal API because I wanted to stay within the .NET ecosystem that I am familiar with from other coursework for an eventual migration to Azure SQL. While the tutorial used TypeScript with `openapi-backend` (which natively routes by `operationId`), ASP.NET Core required a different approach to contract-first development.

### What I Learned About Contract-First Development

The most impactful lesson was that designing the API contract before writing code forces you to think about your data model and operations. When I wrote the OpenAPI YAML first, I had to decide on field types, required vs. optional fields, enum values, and response codes before touching any C# code. This front-loaded design thinking prevented the kind of API evolution that happens when you code first and document later.

I also learned that contract-first in C# is less native than in TypeScript. The `openapi-backend` library in Node.js is built around `operationId` routing and spec-driven validation. In ASP.NET Core, I had to build a custom `OpenApiValidationMiddleware` that parses the YAML spec at startup and validates request bodies against the schemas. It made me understand exactly what spec-driven validation means at a low level, rather than relying on a library to abstract it away.

### Challenges

The biggest challenge was implementing spec-driven validation without a mature C# library for it. I wrote a lightweight YAML parser that extracts the schema information (required fields, types, enums) directly from the spec file. While it is not a full JSON Schema validator, it handles all the validation scenarios required by the assignment.

Another challenge was route ordering: ASP.NET Core's `/{id}` route would match `/summary` if registered first. The fix was simple (register `/summary` before `/{id}`) but it was a subtle bug that only became apparent during testing.

### Contract-First vs. Code-First

Having used Swashbuckle (code-first) in previous projects, I can clearly see the tradeoff. Code-first is faster to get started: you write your controllers, add attributes, and Swashbuckle auto-generates the spec. But the spec becomes a product of your code, not a design document. Contract-first flips this: the spec is the authority, and your code conforms to it. This is better for team collaboration (front-end and back-end can work in parallel from the spec) and for API consumers who need a stable contract. The initial overhead of writing the YAML pays off.
