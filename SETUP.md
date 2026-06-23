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

`Data/DbSeeder.cs` always seeds required roles and reference content. Demo accounts and clinic data are only seeded when both conditions are true:

- `ASPNETCORE_ENVIRONMENT=Development`
- `MOODBITE_SEED_DEMO_DATA=true`

Demo credentials for disposable local demos:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@moodbite.com` | `Demo@123456` |
| ClinicOwner | `clinic.owner@moodbite.demo` | `Demo@123456` |
| Dietitian | `dietitian@moodbite.demo` | `Demo@123456` |
| ClinicStaff | `staff@moodbite.demo` | `Demo@123456` |
| Patient | `patient.one@moodbite.demo` | `Demo@123456` |
| Patient | `patient.two@moodbite.demo` | `Demo@123456` |

The market demo seed creates one active clinic, staff memberships, two linked patients, health profiles, weight/water/food logs, meal plans, clinical notes, and appointments.

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

## AI behavior

Set `GEMINI_API_KEY` to enable live Gemini calls. If it is blank or Gemini is unavailable, the patient and clinic meal-plan flows keep working through the built-in algorithmic meal-plan fallback, workout generation creates a local fallback plan, and the chatbot/scanner return friendly unavailable messages. Prompts and model response previews are not logged.

## Email and invitations

This MVP does not include a production email sender. In Development only:

- Forgot-password flow displays a copyable reset link on the confirmation page.
- Clinic patient invitation flow displays a copyable invite link after creating an invitation.

In Production, configure a real email sender before selling to clinics, then replace the Development-only copy-link behavior with email delivery and audit logging. Do not add SMTP credentials to source control.

## Deploy safely

Set the variables in the deployment environment, not in source-controlled files. Use your platform's secret manager for `GEMINI_API_KEY` and production database credentials.

Do not deploy `.env` files. Rotate the Gemini API key that was previously stored in source, restrict it in the Google AI/Gemini console where possible, and review deployment logs to confirm secrets are not printed.

Use a production SQL Server connection string with least-privilege credentials. Set `ALLOWED_HOSTS` to the production domains instead of `*`, and set `ASPNETCORE_ENVIRONMENT=Production`.

## Market MVP Readiness Status

Current status: demo-ready with controlled local demo seeding, clinic/patient/admin route hardening, graceful AI fallbacks, custom error pages, and clean builds.

Before selling to real clinics:

- Configure production email delivery for password resets and invitations.
- Enable HTTPS-only hosting, secure cookies, backup/restore, monitoring, and log retention.
- Run a real browser QA pass against a seeded database for Admin, ClinicOwner, Dietitian, ClinicStaff, and Patient accounts.
- Review consent language and clinic data access rules with legal/compliance counsel for the target market.
- Replace demo seed passwords after any public demo environment is created.
