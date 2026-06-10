# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

---

## Commands

```bash
# Run development server (with Razor runtime compilation)
dotnet run

# Build
dotnet build

# Apply pending migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Remove last migration (before applying)
dotnet ef migrations remove
```

The app uses SQL Server LocalDB by default (`MoodBiteDb`). The database is seeded on first run via `Data/DbSeeder.cs`.

---

## Configuration

`appsettings.json` holds the Gemini API key under `Gemini:ApiKey` and the SQL Server connection string under `ConnectionStrings:DefaultConnection`. Never read the AI key as an env var — `GeminiService` pulls it via `config["Gemini:ApiKey"]`.

Default admin credentials (seeded): `admin@moodbite.com` / `Admin@123456`

---

## Architecture

**ASP.NET Core MVC on .NET 10** with SQL Server + EF Core. All routes follow the default `{controller}/{action}/{id?}` pattern plus a named `areas` route for `/Admin`.

### Areas

`Areas/Admin/Controllers/` contains admin-only controllers (`[Authorize(Roles = "Admin")]`). Route: `/Admin/AdminDashboard`, `/Admin/AdminUsers`, etc. New admin pages go here.

### Services (all `Scoped`)

| Service | Purpose |
|---------|---------|
| `GeminiService` | All AI calls — meal plan generation, editing, workout generation, food photo analysis, chatbot. Uses `HttpClient` (typed client). Falls back from `gemini-2.5-flash` → `gemini-2.5-flash-lite` on 400/429/503/404. Has a 30-second per-user rate limiter via `IMemoryCache`. |
| `TranslationService` | i18n. Reads `lang` from Session, falling back to cookie, then defaults to `"ar"`. Call `_t.Get("key")` in controllers; pass the service to views or use `ViewBag.T`. |
| `MealPlanService` | Algorithm-based 7-day meal plan from a hard-coded diet food database. |
| `ReportService` | Computes weekly analytics (adherence, avg calories, mood, nutritional gaps) from `DayLog` rows. |
| `NotificationService` | Creates default notifications on first dashboard load; handles Sunday progress reminders. |
| `AchievementService` | Awards achievement badges (streak milestones, challenge completions, etc.). |

### Data model highlights

`ApplicationUser : IdentityUser` — adds `FullName`, `ProfilePicture`, `IsActive`, `PreferredLanguage`, and navigation properties for all per-user data.

`ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser>`. All user-owned entities have `UserId` FK with `DeleteBehavior.Cascade`.

JSON columns: `Diet.BenefitsJson`, `Diet.FoodsJson`, `Diet.SampleMealsJson`, `Diet.TipsJson`, `Challenge.TasksJson`, `CommunityRecipe.IngredientsJson`/`StepsJson`, `MealPlan.PlanJson`, `WorkoutPlan.PlanJson` — these store serialized objects. Deserialize in the controller or service before passing to views.

### Bilingual pattern

Every user-facing string goes through `TranslationService`. All keys live in the `Translations` dictionary inside `Services/TranslationService.cs`. When adding UI text:
1. Add both `ar` and `en` entries to the dictionary.
2. Use `_t.Get("your.key")` in the controller and put the result in `ViewBag` or a ViewModel property.
3. All pages must work in both RTL (Arabic) and LTR (English) — set `dir` attribute from `_t.IsRtl`.

Diet and recipe content is stored bilingually in model properties (`NameAr`/`NameEn`, `TitleAr`/`TitleEn`, etc.). Choose which to display based on `lang`.

### External API: OpenFoodFacts

`ScannerController` uses a named `HttpClient` (`"openfoodfacts"`) to look up products by barcode. Results are saved to `FoodScans` table and can be added to `DayLog`.

### Database seeding

`Data/DbSeeder.cs` runs at startup. It seeds: roles (`Admin`, `User`), the admin user, 8 diets, 6 challenges, 6 community recipes, and 40+ real restaurants across Jordan/Saudi Arabia/UAE/Egypt/Kuwait. Restaurants are re-seeded if the table has fewer than 40 rows.

---

## Key conventions

- **All controllers are `[Authorize]` by default** — unauthenticated users are redirected to `/Account/Login`.
- **CSRF**: All POST actions require `[ValidateAntiForgeryToken]`.
- **Gemini prompts** always request plain JSON (no markdown fences) and parse with `System.Text.Json`. The parser skips `"thought"` parts in the response to handle Gemini's thinking tokens.
- **Adherence** = daily net calories within ±15% of `HealthProfile.CalorieTarget`.
- **CalorieTarget** is stored on `HealthProfile` (pre-computed from Mifflin-St Jeor BMR × activity multiplier during profile save) — do not recompute in the dashboard.
- **Sub-components** for a page go in the same folder when building Razor partials or view components.
