# Audit Logs And Admin Hardening

## Scope Completed

MoodBite now has an `AuditLogs` table, an `IAuditLogService` implementation, admin audit viewing, and clinic-scoped audit viewing for Admin/ClinicOwner users. The existing migration `20260623121245_AddAuditLogs` is reused; no additional audit migration was created in this phase.

## Audit Storage

`AuditLog` stores actor id/email/roles, optional clinic scope, target user/entity identifiers, action, summary, IP address, user agent, UTC timestamp, and bounded metadata JSON. String fields have explicit length limits and indexes cover timestamp, actor, clinic/timestamp, target entity, and action/timestamp lookups.

Metadata is intentionally small and sanitized. Sensitive keys such as passwords, tokens, reset links, API keys, prompts, note content, and medical details are dropped. Suspicious sensitive values are redacted. The service catches audit write failures and logs a warning without breaking the user workflow.

## Audited Admin Actions

- Clinic creation.
- Clinic owner assignment.
- User activation/deactivation.
- User role changes.
- Diet activation/deactivation.
- Community recipe approval/rejection.

## Audited Clinic Actions

- Staff add/update/reactivation.
- Staff activation/deactivation.
- Patient link/reactivation.
- Patient invitation creation and acceptance.
- Patient record detail views.
- Meal plan creation, update, and assignment.
- Clinical note creation, update, view, and archive without note body metadata.
- Appointment creation and update, including status-specific cancelled/completed audit actions.
- Clinic settings updates.

## Audit UI

- `/Admin/AdminAuditLogs` shows recent global audit logs for Admin users only.
- `/Clinic/AuditLogs` shows clinic-scoped logs only for Admin or active ClinicOwner users for that clinic.
- Metadata JSON is not rendered in either UI to avoid exposing sensitive details.
- Filtering supports clinic id, action text, free-text search, and bounded result limits.

## Authorization Hardening

- Admin controllers are role-gated with `ApplicationRoles.Admin`.
- Clinic staff, settings, and clinic audit logs require Admin or ClinicOwner at the tenant level.
- Dietitian and ClinicStaff remain allowed on patient, appointment, note, and meal-plan workspaces but are blocked from owner-only pages.
- Patients are blocked from Clinic and Admin areas.
- Destructive/privileged mutations covered in this phase use POST plus anti-forgery tokens.
- Access denied uses the existing friendly `/Account/AccessDenied` and status code handling.

## Operational Notes

Run `dotnet ef database update` only after explicit deployment approval. Until then, the audit table migration is pending wherever the database has not been updated.
