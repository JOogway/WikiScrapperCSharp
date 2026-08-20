# WikiScrapper

ASP.NET Core MVC prototype that fetches **English and Polish** Wikipedia summaries for all **16 Polish voivodeships** and **world countries** (193 UN members + Vatican City + Palestine — 195 in total), stores them in SQL Server via EF Core, and exposes both a web UI and a documented REST API.

A matching Spring Boot port lives at `IdeaProjects/wikiscrapper` (separate database on the same SQL Express instance).

## Features

- **Bilingual Wikipedia sync** — fetches summaries from both `en.wikipedia.org` and `pl.wikipedia.org` REST APIs (`/api/rest_v1/page/summary/{title}`). Each entity stores EN + PL description, URL, and fetch timestamp. HTTP calls run **in parallel** (bounded by `Wikipedia:MaxConcurrency`, default 8). A worker that receives HTTP 429 waits and retries; other workers keep going. Database writes are **batched** (25 updates per commit). Synchronization is a **background job** (`POST /api/sync` → `202`); poll `GET /api/sync/status` for progress. Overlapping runs return `409`.

- **UI language switch** — navbar English / Polski control sets a `wiki_lang` cookie. The cookie drives both Wikipedia content and UI chrome (shared resources / `.resx`).

- **Web UI** (ASP.NET Core MVC + Bootstrap):
  - **Dashboard** — sync trigger, live progress bar / nav badge, fetch stats, recent audit logs
  - **Voivodeships** — cards with detail modals for the active language
  - **Countries** — search (live AJAX, debounced), status filter, sort, classic pagination, or **page size “All”** with scroll virtualization (chunked `GET /api/countries`)
  - **Logs** — filterable application audit log

- **REST API** (Swagger/OpenAPI + XML comments): `GET /api/voivodeships`, `GET /api/countries`, `GET /api/sync/status`, `POST /api/sync`. Pass `?lang=en` or `?lang=pl` on list/detail endpoints (defaults to English).

- **Structured logging** — Serilog (console + rolling files under `logs/`) plus a database-backed audit log in the UI.

- **Error handling** — global exception middleware (RFC 7807 problem details for API routes); per-item failure isolation during sync.

- **Tests** — xUnit + NSubstitute + FluentAssertions: Wikipedia client (stubbed `HttpMessageHandler`), sync orchestration / job lock, repositories (EF Core In-Memory), sync batch flush behaviour.

## Solution structure

```
WikiScrapper.slnx
WikiScrapper.csproj            MVC host, Domain, Data, Services
  Domain/                      Entities, DTOs, interfaces
  Data/                        EF Core, seed, repositories, DI, sync batch
  Services/                    Wikipedia client, DataSyncService, SyncJobService
  Controllers/ + Controllers/Api/
  Views/, Resources/, wwwroot/
WikiScrapper.Tests/            xUnit + NSubstitute + FluentAssertions + EF InMemory
```

## Prerequisites

- **.NET SDK 10**
- **SQL Server Express** with the **`SQLEXPRESS`** instance running (Windows authentication)

Both this app and the Spring Boot port use the same Express instance but **separate databases** so EF Core and JPA do not clash:

| App | Database |
|-----|----------|
| .NET (this project) | `WikiScrapper` |
| Spring Boot | `WikiScrapperJava` |

Create the databases once (if they do not exist):

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -Q "IF DB_ID('WikiScrapper') IS NULL CREATE DATABASE WikiScrapper; IF DB_ID('WikiScrapperJava') IS NULL CREATE DATABASE WikiScrapperJava;"
```

Verify the instance:

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT @@VERSION"
```

## Database connection

Default in `appsettings.json`:

```
Server=localhost\SQLEXPRESS;Database=WikiScrapper;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Uses **Windows integrated security** against the local `SQLEXPRESS` instance. Change the server or database name there if your instance differs.

Wikipedia concurrency and base URLs live under the `Wikipedia` section of `appsettings.json` (`EnBaseUrl`, `PlBaseUrl`, `MaxConcurrency`).

## Running (fresh machine)

1. Install .NET SDK 10 and ensure SQL Express `SQLEXPRESS` is running.
2. Create the databases with the `sqlcmd` script above (if needed).
3. From the repository root:

```bash
dotnet restore
dotnet run --project WikiScrapper.csproj
```

On startup the app applies EF Core migrations and seeds the 16 voivodeships and 195 countries (idempotent — only when tables are empty).

Then open:

- **Web UI:** https://localhost:7177 (or http://localhost:5275)
- **Swagger UI:** `/swagger`

On the dashboard, press **Synchronize with Wikipedia**. The job runs in the background (progress bar + “Syncing…” badge in the nav). Sync covers **both languages** for every entity (~2× Wikipedia requests vs a single-language run).

Use the language dropdown in the navbar to switch UI + Wikipedia content. On Countries, try search, filters, and page size **All** (virtual scroll).

## Tests

```bash
dotnet test
```

Unit tests use EF Core In-Memory; they do not require SQL Server.

## Migrations

Migrations live in `Data/Migrations` and are applied automatically at startup. To add one:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --project WikiScrapper.csproj --output-dir Data/Migrations
```
