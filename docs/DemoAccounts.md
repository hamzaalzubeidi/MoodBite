# Demo Accounts

Demo data is development-only. It is seeded only when both conditions are true:

- `ASPNETCORE_ENVIRONMENT=Development`
- `MOODBITE_SEED_DEMO_DATA=true`

Do not enable demo seeding in Production. These credentials are intentionally simple and are only for disposable local demos.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@moodbite.com` | `Demo@123456` |
| ClinicOwner | `clinic.owner@moodbite.demo` | `Demo@123456` |
| Dietitian | `dietitian@moodbite.demo` | `Demo@123456` |
| ClinicStaff | `staff@moodbite.demo` | `Demo@123456` |
| Patient/User | `patient.one@moodbite.demo` | `Demo@123456` |
| Patient/User | `patient.two@moodbite.demo` | `Demo@123456` |

Seeded market demo content:

- One realistic clinic: MoodBite Wellness Clinic.
- One clinic owner, one dietitian, and one clinic staff user linked to the clinic.
- Two linked demo patients with consent granted.
- Health profiles, weight logs, water logs, day/nutrition logs, food scans, meal plans, clinical notes, and appointments.

The demo seeder is safe to rerun. It normalizes the documented demo users, passwords, and root roles, then only tops up demo content when rows are missing.
