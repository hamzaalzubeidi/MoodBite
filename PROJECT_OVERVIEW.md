# MoodBite — Complete Project Overview

---

## 1. Project Purpose

**MoodBite** is a bilingual (Arabic/English) AI-powered health and nutrition web application targeting the Arab world. It helps users:

- Build a personalised health profile using a 5-step questionnaire
- Get AI-recommended diet plans (8 types)
- Generate and edit weekly meal plans — both algorithmically and via Google Gemini AI
- Track daily nutrition (calories, macros), mood, weight, and water intake
- View weekly analytics and health reports
- Scan food barcodes (OpenFoodFacts API) and photograph food (Gemini Vision)
- Follow 30-day diet challenges and earn achievement badges
- Connect with a diet buddy for accountability
- Share and discover community recipes
- Find health-friendly restaurants on an interactive map
- Generate personalised AI workout plans
- Access emergency health guidance

The application is fully responsive, mobile-first, and supports both RTL (Arabic, default) and LTR (English) layouts with a dark-first design.

---

## 2. Tech Stack & Dependencies

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | ASP.NET Core MVC | .NET 10 |
| Language | C# | 13 |
| ORM | Entity Framework Core | 10.0.5 |
| Database | SQL Server (LocalDB) | — |
| Auth | ASP.NET Core Identity | 10.0.5 |
| AI | Google Gemini API | REST (via HttpClient) |
| Image processing | SixLabors.ImageSharp | 3.1.8 |
| JSON | System.Text.Json + Newtonsoft.Json | 13.0.4 |
| View compilation | Razor Runtime Compilation | 10.0.5 |
| Frontend CSS | Bootstrap 5.3.3 + custom `site.css` | CDN + local |
| Frontend JS | Bootstrap Bundle, Chart.js 4.4.4, Leaflet 1.9.4, canvas-confetti, Lucide Icons | CDN |
| jQuery | jQuery (for validation only) | local lib |
| External API | OpenFoodFacts | REST |
| Fonts | Inter (LTR) + Cairo (RTL) | Google Fonts CDN |

---

## 3. How to Run

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (included with Visual Studio)

### Steps

```bash
# 1. Clone / open project
cd C:\Users\USER\Desktop\MoodBite

# 2. Restore packages
dotnet restore

# 3. Run (database is created and seeded automatically on first run)
dotnet run

# 4. Open browser
# https://localhost:5001  or  http://localhost:5000
```

### Key Commands

```bash
# Development server (hot Razor reload)
dotnet run

# Build
dotnet build

# Apply pending EF migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Remove the last unapplied migration
dotnet ef migrations remove

# Production publish
dotnet publish -c Release
```

### Default Admin Credentials (seeded automatically)
- **Email:** `admin@moodbite.com`
- **Password:** `Admin@123456`

---

## 4. Configuration

All configuration is in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MoodBiteDb;Trusted_Connection=True"
  },
  "Gemini": {
    "ApiKey": "<your-gemini-api-key>"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "",
    "Password": "",
    "From": "noreply@moodbite.com"
  }
}
```

- The Gemini key is read by `GeminiService` via `config["Gemini:ApiKey"]`.
- Email credentials are blank by default — password reset tokens are stored in `TempData` (not emailed) unless SMTP is configured.

---

## 5. Folder & File Structure

```
MoodBite/
├── Areas/
│   └── Admin/
│       ├── Controllers/
│       │   ├── AdminDashboardController.cs   # Site-wide stats overview
│       │   ├── AdminDietsController.cs       # Toggle diet active/inactive
│       │   ├── AdminRecipesController.cs     # Approve/reject community recipes
│       │   └── AdminUsersController.cs       # Toggle users, change roles
│       └── Views/
│           ├── _ViewImports.cshtml
│           ├── _ViewStart.cshtml             # Points to _AdminLayout
│           ├── Shared/
│           │   └── _AdminLayout.cshtml       # Fixed sidebar + topbar layout
│           ├── AdminDashboard/Index.cshtml
│           ├── AdminDiets/Index.cshtml
│           ├── AdminRecipes/Index.cshtml
│           └── AdminUsers/Index.cshtml
│
├── Controllers/
│   ├── AccountController.cs          # Login, register, logout, forgot/reset password, language toggle
│   ├── AchievementsController.cs     # Achievement list + CheckNew endpoint
│   ├── BuddyController.cs            # Find buddy, send/accept/decline requests
│   ├── ChallengeController.cs        # Browse challenges, join, complete daily task
│   ├── ChatApiController.cs          # /api/Chat — floating chatbot endpoint
│   ├── CommunityController.cs        # Recipe list/detail, like/save, submit recipe
│   ├── CostCalculatorController.cs   # Food budget estimator
│   ├── DashboardController.cs        # Main tracking dashboard + log endpoints
│   ├── DietsController.cs            # Diet catalog, detail page, select diet
│   ├── EmergencyController.cs        # Emergency health guidance (static view)
│   ├── HomeController.cs             # Landing page, privacy, error
│   ├── MealPlanController.cs         # Standard + AI meal plans, shopping list, history
│   ├── NotificationsController.cs    # Notification list, mark read, clear
│   ├── ProfileController.cs          # 5-step health questionnaire, profile picture upload
│   ├── ProgressController.cs         # Body measurements, photo upload, export
│   ├── ReportController.cs           # Weekly analytics report, PDF export
│   ├── RestaurantsController.cs      # Restaurant finder with Leaflet map
│   ├── ScannerController.cs          # Barcode lookup (OpenFoodFacts) + food photo AI analysis
│   ├── WeightController.cs           # Weight log CRUD, chart data
│   └── WorkoutController.cs          # AI workout plan generator
│
├── Data/
│   ├── ApplicationDbContext.cs       # EF Core DbContext; all DbSets and OnModelCreating
│   └── DbSeeder.cs                   # Startup seeder: roles, admin user, diets, challenges, recipes, restaurants
│
├── Migrations/
│   ├── 20260413081051_InitialCreate.*          # All base tables
│   ├── 20260417152345_AddWaterLog.*            # WaterLogs table
│   ├── 20260417210000_AddUserAchievements.*    # UserAchievements table
│   ├── 20260417220000_AddFoodScans.*           # FoodScans table
│   ├── 20260417230000_AddBodyProgress.*        # BodyProgressEntries table
│   ├── 20260508194400_AddWeightNoteAndMealPlanFields.*  # Note on WeightLog; Title/CalorieTarget/DietType on MealPlan
│   ├── 20260508195651_AddNoteToWeightLog.*     # Additional WeightLog note migration
│   └── ApplicationDbContextModelSnapshot.cs   # Current schema snapshot
│
├── Models/
│   ├── ApplicationUser.cs        # IdentityUser extended with FullName, ProfilePicture, IsActive, PreferredLanguage
│   ├── BodyProgress.cs           # Body measurement entry (waist, hips, chest, arms, optional photo)
│   ├── BuddyRequest.cs           # Buddy connection request between two users
│   ├── Challenge.cs              # 30-day challenge template + nested UserChallenge class
│   ├── CommunityRecipe.cs        # User recipe with JSON ingredients/steps + nested RecipeLike/RecipeSave
│   ├── DayLog.cs                 # Daily calorie/macro/mood log
│   ├── Diet.cs                   # Diet plan with JSON fields for bilingual benefits/foods/tips
│   ├── ErrorViewModel.cs         # RequestId for error page
│   ├── FoodScan.cs               # AI food photo analysis result
│   ├── HealthProfile.cs          # Full health questionnaire (5 steps worth of fields)
│   ├── MealPlan.cs               # Standard or AI meal plan JSON + edit history
│   ├── Notification.cs           # Per-user notification
│   ├── Restaurant.cs             # Restaurant with lat/lng coordinates
│   ├── UserAchievement.cs        # Tracks which achievement a user has unlocked
│   ├── WaterLog.cs               # Daily glasses of water count
│   ├── WeightLog.cs              # Daily weight entry with optional note
│   └── WorkoutPlan.cs            # AI-generated workout plan JSON
│
├── Services/
│   ├── AchievementService.cs     # 15 achievements in 5 categories; checks/unlocks per user
│   ├── GeminiService.cs          # All Google Gemini AI calls (meal plan, workout, food vision, chat)
│   ├── MealPlanService.cs        # Algorithm-based meal plan generator; shopping list builder
│   ├── NotificationService.cs    # Default notification seeding; Sunday progress reminders
│   ├── ReportService.cs          # Weekly analytics engine (adherence, macros, mood, water trends)
│   └── TranslationService.cs     # 500+ key AR/EN translation dictionary; reads lang from Session/cookie
│
├── ViewModels/
│   ├── Account/
│   │   ├── ForgotPasswordViewModel.cs
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   └── ResetPasswordViewModel.cs
│   └── DashboardViewModel.cs     # Aggregated data for the dashboard view
│
├── Views/
│   ├── _ViewImports.cshtml       # Global using statements + TagHelpers
│   ├── _ViewStart.cshtml         # Sets _Layout for all views
│   ├── Shared/
│   │   ├── _Layout.cshtml        # Master layout: navbar, footer, scripts, achievement checker
│   │   ├── _Chatbot.cshtml       # Floating chatbot FAB + window (shown when authenticated)
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── Error.cshtml
│   ├── Account/                  # Login, Register, ForgotPassword, ResetPassword, AccessDenied
│   ├── Achievements/Index.cshtml
│   ├── Buddy/Index.cshtml
│   ├── Challenge/Index.cshtml
│   ├── Community/                # Index, Detail, Submit
│   ├── CostCalculator/Index.cshtml
│   ├── Dashboard/Index.cshtml
│   ├── Diets/                    # Index (catalog grid), Detail (individual diet)
│   ├── Emergency/Index.cshtml
│   ├── Home/                     # Index (landing page), Privacy
│   ├── MealPlan/                 # Index (standard + AI tabs), ShoppingList, History
│   ├── Notifications/Index.cshtml
│   ├── Profile/                  # Index (questionnaire form), Result (recommendation card)
│   ├── Progress/Index.cshtml
│   ├── Report/Index.cshtml
│   ├── Restaurants/Index.cshtml
│   ├── Scanner/                  # Index (camera + barcode), MyScanHistory
│   ├── Weight/Index.cshtml
│   └── Workout/                  # Index (plan view), Questionnaire (preferences form)
│
├── wwwroot/
│   ├── css/
│   │   └── site.css              # Full custom dark-first design system (CSS variables, all components)
│   ├── js/
│   │   └── site.js               # Dark mode, tabs, charts, chatbot, circular progress rings, animations
│   ├── lib/
│   │   ├── bootstrap/            # Bootstrap 5 CSS + JS (fallback / validation)
│   │   ├── jquery/               # jQuery (validation only)
│   │   ├── jquery-validation/
│   │   └── jquery-validation-unobtrusive/
│   └── uploads/
│       ├── (profile pictures)
│       └── food-scans/{userId}/  # Food photo scan images per user
│           (progress/{userId}/    # Body progress photos)
│
├── appsettings.json              # Connection string, Gemini API key, SMTP config
├── appsettings.Development.json
├── MoodBite.csproj
└── Program.cs                    # Service registration, middleware pipeline, startup seeding
```

---

## 6. Database Schema

All tables use SQL Server via EF Core. Identity tables are standard. Custom tables:

### `HealthProfiles`
One-to-one with `AspNetUsers`.

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| UserId | string FK | Cascade delete |
| Age | int | |
| Gender | string | "male" / "female" |
| Height | double | cm |
| Weight | double | kg |
| Goal | string | loseWeight / buildMuscle / maintain / improveHealth / manageCondition |
| ActivityLevel | string | sedentary / lightlyActive / moderatelyActive / veryActive / extremelyActive |
| HealthConditions | string? | JSON array of condition keys |
| Allergens | string? | JSON array of allergen keys |
| FoodPreferences | string? | JSON array |
| CookingStyle | string | quick / moderate / adventurous |
| Budget | string | low / medium / high |
| DietSlug | string? | Active diet selection |
| CalorieTarget | double | Pre-computed via Mifflin-St Jeor BMR × activity multiplier |
| WaterGoal | int | Default 8 glasses |
| UpdatedAt | DateTime | |

### `DayLogs`
One-per-day nutrition and mood record.

| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| UserId | string FK | Cascade |
| Date | DateTime | |
| CaloriesConsumed | double | |
| CaloriesBurned | double | |
| Protein | double | grams |
| Carbs | double | grams |
| Fats | double | grams |
| Mood | string? | tired / stressed / needEnergy / veryHungry / cantSleep / postWorkout / sick / great |
| Adherent | bool | net calories within ±15% of CalorieTarget |

### `WeightLogs`
`Id, UserId (FK cascade), Date, Weight (double kg), Note (string? max 500)`

### `WaterLogs`
`Id, UserId (FK cascade), Date, GlassesCount (int)`

### `Diets`
`Id, Slug (unique), NameAr, NameEn, DescriptionAr, DescriptionEn, Category, Difficulty, ColorGradient, Emoji, CalorieMin, CalorieMax, BenefitsJson, FoodsJson, SampleMealsJson, TipsJson, IsActive`

### `MealPlans`
`Id, UserId (FK cascade), PlanType ("standard"/"ai"), PlanJson, EditHistoryJson?, Title?, CalorieTarget, DietType?, CreatedAt`

### `WorkoutPlans`
`Id, UserId (FK cascade), PlanJson, GeneratedAt`

### `CommunityRecipes`
`Id, UserId (FK cascade), TitleAr, TitleEn, DietType, IngredientsJson, StepsJson, Calories, Protein, Carbs, Fats, PrepTime, Likes, IsApproved, CreatedAt`

### `RecipeLikes` / `RecipeSaves`
Join tables: `Id, RecipeId (FK cascade), UserId (FK restrict)`

### `Challenges`
`Id, Slug, NameAr, NameEn, DescriptionAr, DescriptionEn, Difficulty, DietType, Emoji, TasksJson`

### `UserChallenges`
`Id, UserId (FK cascade), ChallengeId (FK cascade), CurrentDay, Streak, StartDate, CompletedDaysJson, LastCheckIn?`

### `Notifications`
`Id, UserId (FK cascade), TitleAr, TitleEn, MessageAr, MessageEn, Type, IsRead, CreatedAt`

### `BuddyRequests`
`Id, SenderId (FK restrict), ReceiverId (FK restrict), Status ("pending"/"accepted"/"declined"), CreatedAt, Message?`

### `Restaurants`
`Id, NameAr, NameEn, Type, City, Country, Latitude, Longitude, Phone?, Address?, Rating`

### `UserAchievements`
`Id, UserId (FK cascade), AchievementKey, UnlockedAt` — unique index on `(UserId, AchievementKey)`

### `FoodScans`
`Id, UserId (FK cascade), ImagePath, FoodNameAr, FoodNameEn, Confidence (int 0–100), Calories, Protein, Carbs, Fats, ServingSize, ServingSizeAr, DescriptionAr?, DescriptionEn?, AlternativesJson?, LoggedToDashboard (bool), ScannedAt`

### `BodyProgressEntries`
`Id, UserId (FK cascade), Date, Weight?, Waist?, Hips?, Chest?, Arms?, Notes?, PhotoPath?, CreatedAt`

---

## 7. Architecture & Design

### Request Pipeline (Program.cs)
```
HTTPS Redirect → Static Files → Routing → Session → Authentication → Authorization
```

### Two Route Patterns
- **Admin area:** `{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}`
- **Default:** `{controller=Home}/{action=Index}/{id?}`

### Authorization
All feature controllers are `[Authorize]`. Unauthenticated requests redirect to `/Account/Login`. Admin controllers add `[Authorize(Roles = "Admin")]`. The `HomeController` and public diet pages are accessible without login.

### Bilingual System
Language is stored in three places (kept in sync):
1. **Session** key `"lang"` — fastest per-request read
2. **Cookie** key `"lang"` — survives session expiry
3. **`ApplicationUser.PreferredLanguage`** — persisted to DB on login and language toggle

`TranslationService.CurrentLang` reads Session → cookie → defaults to `"ar"`. The static `Translations` dictionary inside `TranslationService` holds 500+ keys covering every UI string in both languages.

All data models with user-facing text store separate `*Ar`/`*En` fields. JSON columns in Diet, Challenge, Recipe, etc. use `{"ar": [...], "en": [...]}` structures.

### AI Integration (GeminiService)
All Gemini calls flow through `GeminiService`:
- **Model cascade:** tries `gemini-2.5-flash` first, falls back to `gemini-2.5-flash-lite` on 400/429/503/404 errors.
- **Rate limiting:** 30-second per-user cooldown enforced via `IMemoryCache` before any generation call.
- **JSON enforcement:** all structured calls use `responseMimeType: "application/json"` in the generation config.
- **Thought filtering:** response parsing skips parts where `"thought": true` to handle Gemini's internal reasoning tokens.
- **Vision calls:** food photo analysis uses inline base64 encoding with a separate vision prompt.

### JSON Columns
Diet, Challenge, MealPlan, WorkoutPlan, FoodScan, and CommunityRecipe all store structured data as JSON strings in `nvarchar(max)` columns — there are no separate child tables for these. Deserialise with `JsonSerializer.Deserialize<T>()` in the controller or service before passing to views.

### File Uploads
- **Profile pictures:** `wwwroot/uploads/{filename}`
- **Food scan photos:** `wwwroot/uploads/food-scans/{userId}/{guid}.jpg`
- **Body progress photos:** `wwwroot/uploads/progress/{userId}/{guid}.jpg` — resized to max 800px wide at 85% JPEG quality using ImageSharp.

### Frontend Design System (`wwwroot/css/site.css`)
Dark-first design using CSS custom properties:
- Brand: `--primary: #00C896` (green), `--primary-dark: #00a87f`
- Surfaces: layered dark greys for cards, inputs, modals
- Light theme applied via `[data-theme="light"]` attribute on `<html>`
- Theme persisted in `localStorage['moodbite-theme']` and applied before first paint via inline script in `_Layout.cshtml`
- RTL support via Bootstrap 5's RTL CSS bundle (switched at runtime based on language)
- Components: navbar, footer, cards, buttons, badges, tabs, chatbot window, circular progress rings, forms

### Client-Side JavaScript (`wwwroot/js/site.js`)
Key behaviours:
- **Dark mode toggle** — switches `[data-theme]` on `<html>`, persists to `localStorage`
- **Tab system** — `[data-tabs-container]` / `[data-tab-target]` attribute-driven tab switching
- **Scroll animations** — Intersection Observer fades in cards and stat elements on viewport entry
- **Circular progress rings** — SVG generated dynamically from `[data-progress-ring]` attributes
- **Charts** — `renderCalorieChart()` (bar, colour-coded by adherence) and `renderWeightChart()` (line) via Chart.js
- **Chatbot** — POSTs to `/api/Chat`, shows typing indicator, renders reply
- **Achievement toasts** — `_Layout.cshtml` fetches `/Achievements/CheckNew` on every authenticated page load; shows animated toast + confetti (canvas-confetti) for each new unlock

### Seeded Data
`DbSeeder.cs` runs on every startup and is idempotent (skips if data exists):
- Roles: `Admin`, `User`
- Admin user: `admin@moodbite.com` / `Admin@123456`
- 8 diets: keto, mediterranean, vegan, paleo, dash, intermittent-fasting, flexitarian, carnivore
- 6 challenges: keto, mediterranean, sugar-free, vegan, intermittent-fasting, paleo
- 6 community recipes (approved, authored by admin)
- 50 restaurants with real coordinates across 5 countries: Jordan (Amman), Saudi Arabia (Riyadh), UAE (Dubai), Egypt (Cairo), Kuwait (Kuwait City) — re-seeded if fewer than 40 rows exist

---

## 8. All Routes & Their Purpose

| Method | Route | Controller / Action | Auth | Description |
|--------|-------|---------------------|------|-------------|
| GET | `/` | Home/Index | — | Landing page (redirects to /Dashboard if logged in) |
| GET | `/Account/Login` | Account/Login | — | Login form |
| POST | `/Account/Login` | Account/Login | — | Authenticate user |
| GET | `/Account/Register` | Account/Register | — | Registration form |
| POST | `/Account/Register` | Account/Register | — | Create account |
| POST | `/Account/Logout` | Account/Logout | ✓ | Sign out |
| POST | `/Account/SetLanguage` | Account/SetLanguage | — | Toggle AR/EN |
| GET | `/Dashboard` | Dashboard/Index | ✓ | Main tracking dashboard |
| POST | `/Dashboard/LogCalories` | Dashboard/LogCalories | ✓ | Log today's calories+macros |
| POST | `/Dashboard/LogWeight` | Dashboard/LogWeight | ✓ | Log today's weight |
| POST | `/Dashboard/LogMood` | Dashboard/LogMood | ✓ | Log today's mood |
| GET | `/Dashboard/TodayWater` | Dashboard/TodayWater | ✓ | AJAX: today's water count |
| POST | `/Dashboard/LogWater` | Dashboard/LogWater | ✓ | AJAX: increment/decrement water |
| GET | `/Profile` | Profile/Index | ✓ | Health questionnaire form |
| POST | `/Profile/Save` | Profile/Save | ✓ | Save health profile |
| GET | `/Profile/Result` | Profile/Result | ✓ | Diet recommendation card |
| POST | `/Profile/UploadPicture` | Profile/UploadPicture | ✓ | Upload profile picture |
| GET | `/Diets` | Diets/Index | — | Diet catalog (filtered) |
| GET | `/Diets/Detail/{slug}` | Diets/Detail | — | Individual diet page |
| POST | `/Diets/SelectDiet` | Diets/SelectDiet | ✓ | Set active diet |
| GET | `/MealPlan` | MealPlan/Index | ✓ | Standard + AI meal plans |
| POST | `/MealPlan/Regenerate` | MealPlan/Regenerate | ✓ | New standard plan |
| POST | `/MealPlan/GenerateAI` | MealPlan/GenerateAI | ✓ | Generate AI meal plan |
| POST | `/MealPlan/EditAIPlan` | MealPlan/EditAIPlan | ✓ | AJAX: AI edit meal plan |
| GET | `/MealPlan/ShoppingList` | MealPlan/ShoppingList | ✓ | Shopping list view |
| GET | `/MealPlan/History` | MealPlan/History | ✓ | AI plan history |
| GET | `/MealPlan/Load/{id}` | MealPlan/Load | ✓ | AJAX: load plan JSON |
| DELETE | `/MealPlan/Delete/{id}` | MealPlan/Delete | ✓ | AJAX: delete plan |
| GET | `/Report` | Report/Index | ✓ | Weekly analytics |
| GET | `/Report/ExportPdf` | Report/ExportPdf | ✓ | Download HTML report |
| GET | `/Community` | Community/Index | ✓ | Recipe community |
| GET | `/Community/Detail/{id}` | Community/Detail | ✓ | Recipe detail |
| POST | `/Community/Like` | Community/Like | ✓ | AJAX: toggle like |
| POST | `/Community/Save` | Community/Save | ✓ | AJAX: toggle save |
| GET/POST | `/Community/Submit` | Community/Submit | ✓ | Submit recipe |
| GET | `/Challenge` | Challenge/Index | ✓ | Challenges page |
| POST | `/Challenge/Join` | Challenge/Join | ✓ | Join a challenge |
| POST | `/Challenge/CompleteTask` | Challenge/CompleteTask | ✓ | Mark today's task done |
| GET | `/Buddy` | Buddy/Index | ✓ | Buddy system |
| POST | `/Buddy/SendRequest` | Buddy/SendRequest | ✓ | Send buddy request |
| POST | `/Buddy/AcceptRequest` | Buddy/AcceptRequest | ✓ | Accept buddy request |
| POST | `/Buddy/DeclineRequest` | Buddy/DeclineRequest | ✓ | Decline request |
| GET | `/Workout` | Workout/Index | ✓ | Workout plan view |
| GET | `/Workout/Questionnaire` | Workout/Questionnaire | ✓ | Workout preferences form |
| POST | `/Workout/Generate` | Workout/Generate | ✓ | Generate AI workout |
| GET | `/Scanner` | Scanner/Index | ✓ | Food scanner page |
| GET | `/Scanner/Lookup` | Scanner/Lookup | ✓ | AJAX: barcode lookup |
| POST | `/Scanner/AnalyzePhoto` | Scanner/AnalyzePhoto | ✓ | AJAX: AI food photo analysis |
| POST | `/Scanner/LogScan` | Scanner/LogScan | ✓ | Log scan to dashboard |
| POST | `/Scanner/AddToDashboard` | Scanner/AddToDashboard | ✓ | Log barcode to dashboard |
| GET | `/Scanner/MyScanHistory` | Scanner/MyScanHistory | ✓ | User's scan history |
| GET | `/Notifications` | Notifications/Index | ✓ | Notification center |
| POST | `/Notifications/MarkRead` | Notifications/MarkRead | ✓ | Mark one read |
| POST | `/Notifications/MarkAllRead` | Notifications/MarkAllRead | ✓ | Mark all read |
| POST | `/Notifications/ClearRead` | Notifications/ClearRead | ✓ | Delete all read |
| GET | `/Weight` | Weight/Index | ✓ | Weight log |
| POST | `/Weight/Log` | Weight/Log | ✓ | Add/update weight |
| DELETE | `/Weight/Delete/{id}` | Weight/Delete | ✓ | AJAX: delete entry |
| GET | `/Weight/ChartData` | Weight/ChartData | ✓ | AJAX: last 30 entries |
| GET | `/Progress` | Progress/Index | ✓ | Body measurements |
| POST | `/Progress/Add` | Progress/Add | ✓ | Add measurement entry |
| POST | `/Progress/Delete` | Progress/Delete | ✓ | Delete entry |
| GET | `/Progress/ExportPdf` | Progress/ExportPdf | ✓ | Export measurements |
| GET | `/Restaurants` | Restaurants/Index | ✓ | Restaurant map |
| GET | `/Emergency` | Emergency/Index | ✓ | Emergency guidance |
| GET | `/CostCalculator` | CostCalculator/Index | ✓ | Food budget calculator |
| GET | `/Achievements` | Achievements/Index | ✓ | Achievement list |
| GET | `/Achievements/CheckNew` | Achievements/CheckNew | ✓ | AJAX: check new unlocks |
| POST | `/api/Chat` | ChatApi/Post | ✓ | Floating chatbot |
| GET | `/Admin` | Admin/AdminDashboard/Index | Admin | Admin stats |
| GET | `/Admin/AdminUsers` | Admin/AdminUsers/Index | Admin | User management |
| POST | `/Admin/AdminUsers/ToggleActive` | — | Admin | Activate/deactivate user |
| POST | `/Admin/AdminUsers/ChangeRole` | — | Admin | Change user role |
| GET | `/Admin/AdminRecipes` | Admin/AdminRecipes/Index | Admin | Recipe moderation |
| POST | `/Admin/AdminRecipes/Approve` | — | Admin | Approve recipe |
| POST | `/Admin/AdminRecipes/Reject` | — | Admin | Reject recipe |
| GET | `/Admin/AdminDiets` | Admin/AdminDiets/Index | Admin | Diet management |
| POST | `/Admin/AdminDiets/ToggleActive` | — | Admin | Toggle diet visibility |

---

## 9. Services Reference

### `GeminiService`
Singleton-like typed `HttpClient`. All methods enforce rate limit before calling Gemini.

| Method | Input | Output |
|--------|-------|--------|
| `GenerateMealPlanAsync` | HealthProfile, dietSlug, lang | 7-day plan JSON string |
| `EditMealPlanAsync` | existingPlanJson, instruction, lang | Modified plan JSON string |
| `GenerateWorkoutPlanAsync` | HealthProfile + optional preferences | 7-day workout JSON string |
| `AnalyzeFoodPhotoAsync` | imageBytes, mimeType, lang | Food analysis JSON string |
| `ChatAsync` | userMessage, context, lang | Plain text reply (≤150 words) |

### `TranslationService`
Scoped. Key method: `Get(key)` → string in current language. `IsRtl` → bool.

### `MealPlanService`
Scoped. `GenerateWeekPlan(dietSlug, seed)` → JSON string. `GenerateShoppingList(planJson)` → JSON string with 5 category lists.

### `ReportService`
Scoped. `GetWeeklyReportAsync(userId, calorieTarget, waterGoal)` → `ReportData` object containing: adherencePercent, avgCalories, trend (week-over-week delta), topMood, moodFrequency, macro averages, daily series arrays, `HasEnoughData` flag.

### `NotificationService`
Scoped. Creates 11 default notifications on first dashboard visit. Adds Sunday progress reminders automatically.

### `AchievementService`
Scoped. 15 achievements across 5 categories (streak, nutrition, hydration, mood, explorer). `CheckAndUnlockAsync(userId)` evaluates all conditions and persists new unlocks. The `_Layout.cshtml` calls `/Achievements/CheckNew` on every page load for authenticated users.

---

## 10. Important Notes

### CSRF
Every POST action has `[ValidateAntiForgeryToken]`. All AJAX POSTs must include the anti-forgery token (retrieved from the hidden `__RequestVerificationToken` input or the `RequestVerificationToken` cookie).

### Calorie Target Computation
`ProfileController.CalculateCalorieTarget` uses Mifflin-St Jeor BMR × activity multiplier. Adjustments: lose weight → TDEE − 500 kcal, build muscle → TDEE + 300 kcal. This value is stored on `HealthProfile.CalorieTarget` and never recomputed on the dashboard — always read it from the profile.

### Diet Recommendation Logic
`ProfileController.RecommendDiet` is rule-based, not AI:
- Heart disease or hypertension → DASH
- Diabetes → Mediterranean
- Lose weight goal → Keto
- Build muscle goal → Paleo
- Gluten or dairy allergy → Vegan
- Default → Mediterranean

### Adherence Definition
A day is `Adherent = true` when `|net calories − CalorieTarget| / CalorieTarget ≤ 0.15` (within ±15%).

### JSON Column Pattern
When reading data from JSON-column fields (e.g., `Diet.FoodsJson`, `MealPlan.PlanJson`), always `JsonSerializer.Deserialize<T>()` in the controller before passing to the view. Views should never deserialise raw JSON.

### Gemini Response Parsing
The parser explicitly skips response parts where `"thought": true` — this is required because Gemini 2.5 Flash may include internal reasoning tokens in its response that must be ignored to get the actual output.

### Language Toggle Sequence
When a user clicks the language toggle button, the form POSTs to `Account/SetLanguage`. That action:
1. Saves to Session `"lang"`
2. Saves to cookie `"lang"` (30-day expiry)
3. Updates `user.PreferredLanguage` in the database (if authenticated)
4. Redirects back to the referring page

### Email / Password Reset
Email sending is not implemented — SMTP credentials are blank. Password reset tokens are stored in `TempData` as a workaround. To enable real email reset, populate `Email:Username` and `Email:Password` in `appsettings.json` and implement the send logic in `AccountController.ForgotPassword`.

### Restaurant Data
All 50 restaurants are real establishments with real coordinates and phone numbers across Amman, Riyadh, Dubai, Cairo, and Kuwait City. The seeder re-runs if fewer than 40 records exist (replaces any old placeholder data).

### Upload Security
Profile picture filenames are sanitised with `Path.GetExtension` and stored with a `Guid` prefix. Food scan and progress photos are similarly GUID-named. Only image MIME types should be accepted (validate in controller before saving).
