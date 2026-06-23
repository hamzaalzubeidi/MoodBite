# MoodBite Authenticated Role QA Checklist

Use this checklist against a Development database with demo seeding enabled:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:MOODBITE_SEED_DEMO_DATA="true"
dotnet run
```

Do not run demo seeding in Production.

## Demo Accounts

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@moodbite.com` | `Demo@123456` |
| ClinicOwner | `clinic.owner@moodbite.demo` | `Demo@123456` |
| Dietitian | `dietitian@moodbite.demo` | `Demo@123456` |
| ClinicStaff | `staff@moodbite.demo` | `Demo@123456` |
| Patient | `patient.one@moodbite.demo` | `Demo@123456` |
| Patient | `patient.two@moodbite.demo` | `Demo@123456` |

## Admin Flow

- Log in as the demo admin.
- Open `/Admin`.
- Open Admin dashboard and confirm summary cards render.
- Open Admin users.
- Toggle a non-current user's active status, then restore it.
- Change a demo user's root role only where the UI allows it.
- Open Admin clinics.
- Inspect the demo clinic row.
- Create a temporary demo clinic if needed.
- Assign a ClinicOwner by email.
- Open clinic settings/staff links from Admin clinics.
- Verify all Admin sidebar links stay under `/Admin` except Main Site.
- Verify no click shows a 404, raw exception, or Developer Exception Page.

## Clinic Owner Flow

- Log in as `clinic.owner@moodbite.demo`.
- Open `/Clinic`.
- Confirm dashboard metrics and empty states render.
- Open settings.
- Update a harmless clinic field, then restore it.
- Open staff.
- Add or reactivate a Dietitian/ClinicStaff demo user if needed.
- Open patients.
- Invite a patient and confirm a copyable invite link appears in Development.
- Link an existing patient by email if needed.
- Open patient details.
- Open the patient's clinic meal plans.
- Create a meal plan and save it.
- Open the meal plan details page.
- Create a clinical note.
- Edit and archive a clinical note.
- Create an appointment.
- Edit appointment status.
- Verify no navigation breaks and no unrelated clinic data appears.

## Dietitian Flow

- Log in as `dietitian@moodbite.demo`.
- Open `/Clinic`.
- Open patient roster.
- Open an assigned patient details page.
- Open meal plans and review the latest plan.
- Create or edit a meal plan if the UI permits it.
- Add a clinical note.
- View appointments.
- Try opening `/Clinic/ClinicSettings`.
- Confirm owner-only management is blocked with a friendly 403 state, not a crash.

## Clinic Staff Flow

- Log in as `staff@moodbite.demo`.
- Open `/Clinic`.
- Open allowed clinic pages such as patients and appointments.
- Try opening `/Clinic/ClinicSettings` and `/Clinic/ClinicStaff`.
- Confirm blocked owner-only pages show a friendly 403 state.
- Confirm patient data is scoped to the assigned clinic only.
- Verify no raw exception or Developer Exception Page appears.

## Patient Flow

- Register a new user or log in as `patient.one@moodbite.demo`.
- Complete `/Profile` if prompted.
- Open `/Dashboard`.
- Log calories, mood, water, and weight.
- Open `/MealPlan`.
- Regenerate a standard plan.
- Try AI meal plan generation with no Gemini key and confirm fallback behavior is friendly.
- Open `/MealPlan/History` and `/MealPlan/ShoppingList`.
- Open `/Scanner`.
- Try barcode/photo failure paths and confirm friendly messages.
- Open `/Weight`, `/Progress`, `/Report`, `/Workout`, `/Restaurants`, `/Emergency`, `/Notifications`, `/Community`, `/Challenge`, `/Buddy`, and `/Achievements`.
- Accept a clinic invitation link generated in Development.
- Confirm the patient portal still works after clinic linking.
- Confirm the patient cannot open `/Admin` or clinic management pages.

## Pass Criteria

- No 404 for valid UI clicks.
- No 500 or Developer Exception Page.
- Forms show validation or success/error messages.
- Access-denied states are friendly.
- Clinic data remains tenant-scoped.
- Patient portal remains usable after clinic linking.
