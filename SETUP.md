# MoodBite Setup Guide

## Project root

The real application is the root project at `MoodBite.csproj`. The nested `MoodBite\` folder is an excluded copy and must not be edited for application changes.

## Create a local .env file

Copy the template and edit the new `.env` file:

```powershell
Copy-Item .env.example .env
notepad .env
```

Do not commit `.env`. It is ignored because it can contain secrets and machine-specific values.

## Configure variables

Set these values in `.env` for local development, or in your hosting platform for deployment:

| Variable | Used by |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment selection. Use `Development` locally and `Production` in production. |
| `ASPNETCORE_URLS` | Kestrel listening URLs when running without `launchSettings.json`. |
| `ALLOWED_HOSTS` | ASP.NET Core host filtering. Use real host names in production. |
| `MOODBITE_DB_CONNECTION_STRING` | EF Core `ApplicationDbContext` SQL Server connection string. |
| `ConnectionStrings__DefaultConnection` | Optional native ASP.NET Core alternative to `MOODBITE_DB_CONNECTION_STRING`. |
| `GEMINI_API_KEY` | Gemini API key, mapped to `Gemini:ApiKey` for `GeminiService`. |
| `GEMINI_HTTP_TIMEOUT_SECONDS` | Timeout for the `GeminiService` typed `HttpClient`. |
| `OPENFOODFACTS_BASE_URL` | Base URL for barcode lookup requests. |
| `OPENFOODFACTS_USER_AGENT` | OpenFoodFacts `User-Agent` header. Use a monitored contact email. |
| `OPENFOODFACTS_TIMEOUT_SECONDS` | Timeout for the named OpenFoodFacts `HttpClient`. |
| `MOODBITE_SEED_DEMO_DATA` | Opt-in prototype/demo seeding. Keep `false` outside disposable local demos. |
| `LOG_LEVEL_DEFAULT` | Default ASP.NET Core logging level. |
| `LOG_LEVEL_ASPNETCORE` | Logging level for `Microsoft.AspNetCore` categories. |

The local `.env` loader does not overwrite variables already set by the operating system or hosting platform.

`Data/DbSeeder.cs` always seeds required roles and reference content. It only seeds unsafe demo/default accounts when `MOODBITE_SEED_DEMO_DATA=true`.

## Run locally

Build the project:

```powershell
dotnet build
```

Apply pending migrations if the database has not been created:

```powershell
dotnet ef database update
```

Start the application:

```powershell
dotnet run
```

With the default launch profile, the app listens on `http://localhost:5298` and `https://localhost:7239`.

## Deploy safely

Set the variables in the deployment environment, not in source-controlled files. Use your platform's secret manager for `GEMINI_API_KEY` and production database credentials.

Do not deploy `.env` files. Rotate the Gemini API key that was previously stored in source, restrict it in the Google AI/Gemini console where possible, and review deployment logs to confirm secrets are not printed.

Use a production SQL Server connection string with least-privilege credentials. Set `ALLOWED_HOSTS` to the production domains instead of `*`, and set `ASPNETCORE_ENVIRONMENT=Production`.
