# Deployment Guide

MoodBite can be deployed as an ASP.NET Core MVC application backed by SQL Server. Production values must come from the hosting platform environment, a secret manager, or secure app configuration. Do not commit `.env`, API keys, SMTP passwords, database passwords, reset links, or invitation links.

## Local Development Run

1. Install the .NET SDK used by the solution.
2. Copy `.env.example` to `.env` for local-only values.
3. Keep `ASPNETCORE_ENVIRONMENT=Development` locally.
4. Run `dotnet build MoodBite.slnx`.
5. Run `dotnet run` from the repository root.
6. Optional demo seed: set `MOODBITE_SEED_DEMO_DATA=true` only for disposable development/demo databases.

## Production Environment Variables

Required or strongly recommended:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS` as required by the host
- `ALLOWED_HOSTS` with explicit production domains
- `MOODBITE_DB_CONNECTION_STRING` or `ConnectionStrings__DefaultConnection`
- `GEMINI_API_KEY` when AI features should be live
- `OPENFOODFACTS_BASE_URL`
- `OPENFOODFACTS_USER_AGENT` with a monitored contact
- `OPENFOODFACTS_TIMEOUT_SECONDS`
- `EMAIL_PROVIDER=Smtp` when email is live
- `EMAIL_FROM`, `EMAIL_FROM_NAME`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_ENABLE_SSL`
- `RATE_LIMIT_AUTH_PERMIT_LIMIT`, `RATE_LIMIT_AUTH_WINDOW_SECONDS`
- `RATE_LIMIT_AI_PERMIT_LIMIT`, `RATE_LIMIT_AI_WINDOW_SECONDS`
- `RATE_LIMIT_SCANNER_PERMIT_LIMIT`, `RATE_LIMIT_SCANNER_WINDOW_SECONDS`

Never enable `MOODBITE_SEED_DEMO_DATA` in production. Demo seeding is development-only, but production configuration should still leave it unset or false.

## Database Setup

Use SQL Server with encrypted connections where supported. Apply EF migrations through the normal deployment process before routing live traffic. Configure automated backups and perform a restore test before launch.

## Email Provider Setup

MoodBite uses `IEmailService` for password reset and clinic invitation emails. Development returns copy-link previews. Production never exposes reset or invitation links directly to users.

Production email requires configured SMTP settings. If email is missing, the app logs a warning and shows safe user messaging instead of crashing or exposing tokens. Before live clinic onboarding, verify sender domain, SPF, DKIM, DMARC, bounce handling, and support inbox ownership.

## Gemini Setup

Set `GEMINI_API_KEY` through secure configuration only. Do not commit it. Monitor quota, latency, fallback usage, and failed calls. AI failures should degrade to existing fallback behavior where supported.

## HTTPS Requirement

Production must terminate HTTPS at the hosting platform or reverse proxy and forward requests to Kestrel securely. Keep HSTS enabled in production. Configure secure cookies and host filtering for the final domain.

## Reverse Proxy Notes

When deploying behind IIS, Nginx, Apache, Azure App Service, or another proxy, ensure forwarded headers and HTTPS termination are configured by the platform. Restrict direct database access to the app environment.

## Health Checks

- `/health` returns basic app liveness without dependency details.
- `/health/ready` includes database connectivity status and returns only a generic status payload.

These endpoints do not expose connection strings, secrets, stack traces, or provider diagnostics.

## Logging And Monitoring

Use structured application logs and central collection. Recommended alerts include high error rate, database connectivity failure, email delivery failure, Gemini failures/quota, scanner API failures, and unusual auth traffic. Do not log passwords, API keys, tokens, reset links, invitation links, full AI prompts, or raw medical notes.

## Backup Recommendations

Create scheduled database backups with retention, encryption, and restore verification. Keep a documented restore runbook and define the recovery point and recovery time targets before onboarding real clinics.

## What Not To Commit

- `.env`
- real SMTP credentials
- Gemini keys
- production SQL credentials
- reset or invitation links
- exported patient data
- production logs containing personal health information

## Pre-Deployment Checklist

- Build passes with 0 warnings and 0 errors.
- Tests pass.
- `git diff --check` is clean except acceptable CRLF warnings.
- Production environment variables are set outside git.
- Email provider has been verified end to end.
- Database migrations are applied to a staging copy first.
- Backups and restore test are complete.
- Health endpoints are reachable by the deployment platform.
- Admin accounts are hardened.
- Privacy, consent, and legal review are complete.

## Post-Deployment Smoke Test Checklist

- `/health` returns healthy.
- `/health/ready` returns healthy after database migration.
- Homepage and login page load over HTTPS.
- Password reset sends email without exposing the link on screen.
- Clinic invitation sends email without exposing the link on screen.
- Admin can open dashboard, users, and clinics.
- Clinic owner can open dashboard, patients, staff, settings, appointments, notes, and meal plans.
- Patient can open dashboard, profile, meal plan, scanner, report, progress, weight, and water pages.
- Gemini fallback and scanner failure messaging are friendly.
- Logs contain no secrets, tokens, reset links, or invitation links.