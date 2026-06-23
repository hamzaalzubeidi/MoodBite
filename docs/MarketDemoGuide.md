# Market Demo Guide

## Run Locally

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:MOODBITE_SEED_DEMO_DATA="true"
dotnet build MoodBite.slnx
dotnet run --project MoodBite.csproj
```

The default launch profile listens on `http://localhost:5298`.

## Demo Accounts

All demo accounts use `Demo@123456`.

| Role | Email |
| --- | --- |
| Admin | `admin@moodbite.com` |
| ClinicOwner | `clinic.owner@moodbite.demo` |
| Dietitian | `dietitian@moodbite.demo` |
| ClinicStaff | `staff@moodbite.demo` |
| Patient | `patient.one@moodbite.demo` |
| Patient | `patient.two@moodbite.demo` |

## Role Capabilities

- Admin: platform dashboard, user management, clinic management, diet and community recipe review.
- ClinicOwner: clinic dashboard, settings, staff management, patient roster, notes, meal plans, appointments.
- Dietitian: clinic dashboard, patient roster, patient details, meal plans, notes, and appointments. Owner-only settings and staff management show friendly 403 pages.
- ClinicStaff: clinic dashboard, patients and appointments according to clinic access. Owner-only settings and staff management show friendly 403 pages.
- Patient/User: dashboard, profile/results, diets, meal plans, shopping list, scanner history, reports, progress, weight/water logging, workout, restaurants, emergency, notifications, community, challenges, buddy, and achievements.

## Suggested Demo Script

Admin demo:

1. Log in as `admin@moodbite.com`.
2. Open `/Admin` and show platform totals.
3. Open Admin Users and Admin Clinics.
4. Show MoodBite Wellness Clinic and the linked clinic management links.

Clinic owner demo:

1. Log in as `clinic.owner@moodbite.demo`.
2. Open `/Clinic`, then settings, staff, and patients.
3. Open a patient detail page, review logs, meal plans, notes, and appointments.
4. Show that owner can manage staff and clinic settings.

Dietitian demo:

1. Log in as `dietitian@moodbite.demo`.
2. Open `/Clinic`, patients, a patient detail page, notes, appointments, and meal plan creation/review.
3. Try `/Clinic/ClinicSettings?clinicId=2` and show the friendly access denied page.

Patient demo:

1. Log in as `patient.one@moodbite.demo`.
2. Open dashboard, profile result, meal plan, meal plan history, shopping list, scanner history, report, progress, and weight.
3. Show clinic linking does not break the patient portal.
4. Try `/Clinic` and show friendly access denied.

## Known Demo Limitations

- Real email provider is not configured. Development flows show copyable reset/invite links instead.
- Real Gemini key is not included. AI features use safe fallbacks or friendly unavailable messages when no key is configured.
- Real production hosting is not configured.

## Safe To Show Now

- Local role-based demo with Admin, ClinicOwner, Dietitian, ClinicStaff, and Patient accounts.
- Clinic dashboard, patient roster, linked patient data, notes, appointments, meal plans, and patient portal flows.
- Friendly fallback behavior for missing email/Gemini services.

## Before Charging Real Customers

- Configure production email delivery, secrets, backups, monitoring, HTTPS-only hosting, audit logs, and legal/privacy review.
- Run a full browser QA pass on a production-like environment.
- Replace demo credentials and disable demo seeding.
- Review clinical claims and consent wording with qualified counsel and nutrition/medical stakeholders.
