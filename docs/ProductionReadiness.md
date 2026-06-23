# Production Readiness

MoodBite is market-demo ready locally, but not paid-production ready until this checklist is completed.

## Infrastructure And Secrets

- [ ] Configure `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Keep `MOODBITE_SEED_DEMO_DATA=false` or unset in production.
- [ ] Store SQL Server credentials and `GEMINI_API_KEY` in a real secret manager.
- [ ] Set `ALLOWED_HOSTS` to production domains.
- [ ] Enforce HTTPS only, HSTS, and secure cookie settings.
- [ ] Configure deployment environment variables explicitly and document ownership.

## Email

- [ ] Add a real email provider for password resets and clinic invitations.
- [ ] Use verified sender domains and SPF/DKIM/DMARC.
- [ ] Add retry/error handling and operational alerts for failed delivery.
- [ ] Remove Development-only copy-link behavior from production flows.

## AI And External APIs

- [ ] Configure a production Gemini key with billing, quota, and key restrictions.
- [ ] Monitor Gemini errors, latency, quota, and fallback usage.
- [ ] Review prompts and outputs for nutrition/medical safety.
- [ ] Confirm OpenFoodFacts timeout, user-agent, rate behavior, and failure messaging.

## Data Protection

- [ ] Configure automated SQL Server backups and test restore.
- [ ] Define retention and deletion policies for health, scan, note, and appointment data.
- [ ] Complete privacy, consent, and legal review for target markets.
- [ ] Add audit logs for admin actions, clinic membership changes, patient links, notes, and appointment changes.
- [ ] Harden admin accounts with strong passwords and MFA or equivalent controls.

## Reliability And Operations

- [ ] Add structured production logging without secrets or sensitive prompts.
- [ ] Add monitoring for uptime, latency, errors, database health, and external API failures.
- [ ] Add error tracking with release/version correlation.
- [ ] Configure rate limiting for auth, AI, scanner, and high-write endpoints.
- [ ] Define incident response and support escalation paths.

## QA Before Launch

- [ ] Run full role QA as Admin, ClinicOwner, Dietitian, ClinicStaff, and Patient.
- [ ] Run form submission QA for account, patient, clinic, and admin forms.
- [ ] Verify Arabic RTL and English LTR layouts on desktop and mobile.
- [ ] Verify access denied pages are friendly and tenant boundaries are enforced.
- [ ] Run `dotnet build MoodBite.slnx`, `dotnet test MoodBite.slnx --no-restore`, and `git diff --check`.
