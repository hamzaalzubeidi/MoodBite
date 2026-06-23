# Production Readiness

MoodBite is market-demo ready locally and closer to production foundation after the email, configuration, rate limiting, health check, and logging-safety work. It is still not paid-production ready until the operational and legal checklist below is completed.

## Implemented Foundation

- [x] Development-only demo seed guard with `MOODBITE_SEED_DEMO_DATA`.
- [x] Email service abstraction for password reset and clinic invitations.
- [x] Development email copy-link preview without production link exposure.
- [x] Production-safe email failure path when provider config is missing.
- [x] Environment variable aliases for database, Gemini, OpenFoodFacts, email, logging, and rate limiting.
- [x] Startup configuration validation warnings without logging secret values.
- [x] Configurable rate limiting for auth, invitation, AI, and scanner endpoints.
- [x] Basic `/health` and readiness `/health/ready` endpoints.
- [x] Tests for email fallback/config safety and health endpoint smoke coverage.

## Infrastructure And Secrets

- [ ] Configure `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Keep `MOODBITE_SEED_DEMO_DATA=false` or unset in production.
- [ ] Store SQL Server credentials, `GEMINI_API_KEY`, and SMTP password in a real secret manager or secure host config.
- [ ] Set `ALLOWED_HOSTS` to explicit production domains.
- [ ] Enforce HTTPS only, HSTS, and secure cookie settings.
- [ ] Configure deployment environment variables explicitly and document ownership.

## Email

- [ ] Configure a real SMTP provider for password resets and clinic invitations.
- [ ] Use verified sender domains and SPF/DKIM/DMARC.
- [ ] Verify reset and invitation emails end to end in staging.
- [ ] Add operational alerts for repeated delivery failures.
- [ ] Define bounce, support inbox, and sender reputation monitoring ownership.

## AI And External APIs

- [ ] Configure a production Gemini key with billing, quota, and key restrictions.
- [ ] Monitor Gemini errors, latency, quota, and fallback usage.
- [ ] Review prompts and outputs for nutrition/medical safety.
- [ ] Confirm OpenFoodFacts timeout, user-agent, rate behavior, and failure messaging.

## Data Protection

- [ ] Configure automated SQL Server backups and test restore.
- [ ] Define retention and deletion policies for health, scan, note, and appointment data.
- [ ] Complete privacy, consent, and legal review for target markets.
- [x] Add audit logs for admin actions, clinic membership changes, patient links, notes, and appointment changes.
- [ ] Harden admin accounts with strong passwords and MFA or equivalent controls.

## Reliability And Operations

- [ ] Connect structured production logging to a central log platform.
- [ ] Add monitoring for uptime, latency, errors, database health, and external API failures.
- [ ] Add error tracking with release/version correlation.
- [ ] Review and tune rate limits after staging traffic tests.
- [ ] Define incident response and support escalation paths.

## QA Before Launch

- [ ] Run full role QA as Admin, ClinicOwner, Dietitian, ClinicStaff, and Patient in staging.
- [ ] Run form submission QA for account, patient, clinic, and admin forms in staging.
- [ ] Verify Arabic RTL and English LTR layouts on desktop and mobile.
- [ ] Verify access denied pages are friendly and tenant boundaries are enforced.
- [ ] Run `dotnet build MoodBite.slnx`, `dotnet test MoodBite.slnx --no-restore`, and `git diff --check`.
## Audit And Admin Hardening

- [x] Reused migration `20260623121245_AddAuditLogs`; database update is still pending until explicitly run.
- [x] Added safe audit service with bounded metadata and sensitive key/value redaction.
- [x] Added Admin and ClinicOwner audit log pages without metadata exposure.
- [x] Added role-boundary tests for Admin, Patient, ClinicOwner, Dietitian, and ClinicStaff routes.
- [ ] Define audit retention, export, and review procedures before paid production.
- [ ] Add centralized monitoring/alerting for suspicious admin and clinic actions.
