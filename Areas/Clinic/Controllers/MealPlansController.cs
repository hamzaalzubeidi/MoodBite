using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Areas.Clinic.Controllers
{
    [Area("Clinic")]
    [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
    public class MealPlansController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;
        private readonly MealPlanService _mealPlanService;
        private readonly GeminiService _geminiService;
        private readonly TranslationService _t;
        private readonly ILogger<MealPlansController> _logger;

        public MealPlansController(
            ApplicationDbContext db,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess,
            MealPlanService mealPlanService,
            GeminiService geminiService,
            TranslationService t,
            ILogger<MealPlansController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
            _mealPlanService = mealPlanService;
            _geminiService = geminiService;
            _t = t;
            _logger = logger;
        }

        [HttpGet("/Clinic/MealPlans")]
        public async Task<IActionResult> Index(int? clinicId, CancellationToken cancellationToken = default)
        {
            var model = new ClinicMealPlansIndexViewModel();

            try
            {
                var resolvedClinicId = await ResolveAccessibleClinicIdAsync(clinicId, cancellationToken);
                if (!resolvedClinicId.HasValue)
                {
                    return View(model);
                }

                var clinic = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == resolvedClinicId.Value && c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return View(model);
                }

                model.HasClinicContext = true;
                model.ClinicId = clinic.Id;
                model.ClinicName = clinic.Name;

                var patients = await _db.ClinicPatients.AsNoTracking()
                    .Where(p => p.ClinicId == clinic.Id)
                    .Select(p => new
                    {
                        p.PatientId,
                        p.Patient.FullName,
                        p.Patient.Email
                    })
                    .ToListAsync(cancellationToken);

                var patientIds = patients.Select(p => p.PatientId).ToList();
                if (patientIds.Count == 0)
                {
                    return View(model);
                }

                var patientMap = patients.ToDictionary(p => p.PatientId);
                var latestIds = await GetLatestPlanIdsByPatientAsync(patientIds, cancellationToken);

                model.TotalPlans = await _db.MealPlans.AsNoTracking()
                    .CountAsync(m => patientIds.Contains(m.UserId), cancellationToken);

                model.PatientsWithPlans = latestIds.Count;

                var recentPlans = await _db.MealPlans.AsNoTracking()
                    .Where(m => patientIds.Contains(m.UserId))
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(30)
                    .ToListAsync(cancellationToken);

                model.RecentPlans = recentPlans
                    .Where(plan => patientMap.ContainsKey(plan.UserId))
                    .Select(plan =>
                    {
                        var patient = patientMap[plan.UserId];
                        return BuildPlanListItem(
                            plan,
                            clinic.Id,
                            patient.FullName,
                            patient.Email,
                            latestIds.GetValueOrDefault(plan.UserId) == plan.Id);
                    })
                    .ToList();
            }
            catch (DbException ex)
            {
                model.ClinicDataUnavailable = true;
                _logger.LogWarning(ex, "Clinic meal plan data is unavailable. The clinic migration may be pending.");
            }

            return View(model);
        }

        [HttpGet("/Clinic/MealPlans/Patient/{patientId}")]
        public async Task<IActionResult> Patient(
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return NotFound();
            }

            var plans = await _db.MealPlans.AsNoTracking()
                .Where(m => m.UserId == access.PatientId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(30)
                .ToListAsync(cancellationToken);

            var latestPlanId = plans.FirstOrDefault()?.Id;

            return View(new ClinicMealPlanPatientViewModel
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = summary,
                Plans = plans
                    .Select(plan => BuildPlanListItem(
                        plan,
                        access.ClinicId,
                        summary.FullName,
                        summary.Email,
                        latestPlanId == plan.Id))
                    .ToList()
            });
        }

        [HttpGet("/Clinic/MealPlans/Create/{patientId}")]
        public async Task<IActionResult> Create(
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var model = await BuildCreateModelAsync(access, cancellationToken);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost("/Clinic/MealPlans/Create/{patientId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string patientId,
            ClinicMealPlanInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var access = await ResolvePatientAccessAsync(patientId, input.ClinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return NotFound();
            }

            var profile = await _db.HealthProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == access.PatientId, cancellationToken);

            var saveResult = await BuildPlanForSaveAsync(input, profile, summary, cancellationToken);
            if (!saveResult.Success)
            {
                TempData["Error"] = saveResult.Message ?? _t.Get("common.error");
                return View(BuildEditorModel(access, summary, input, isNew: true, isLatestAssigned: false));
            }

            var plan = new MealPlan
            {
                UserId = access.PatientId,
                PlanType = saveResult.PlanType,
                PlanJson = saveResult.PlanJson,
                Title = saveResult.Title,
                DietType = saveResult.DietType,
                CalorieTarget = saveResult.CalorieTarget,
                CreatedAt = DateTime.UtcNow
            };

            _db.MealPlans.Add(plan);
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] = saveResult.UsedFallback
                ? _t.Get("clinic.mealPlans.generatedWithFallback")
                : _t.Get("clinic.mealPlans.saved");

            return RedirectToAction(
                nameof(Details),
                new { id = plan.Id, clinicId = access.ClinicId });
        }

        [HttpGet("/Clinic/MealPlans/Edit/{id:int}")]
        public async Task<IActionResult> Edit(
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.MealPlans.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (plan == null)
            {
                return NotFound();
            }

            var access = await ResolvePatientAccessAsync(plan.UserId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return NotFound();
            }

            var latestPlanId = await GetLatestPlanIdAsync(access.PatientId, cancellationToken);
            return View(BuildEditorModel(access, summary, plan, latestPlanId == plan.Id));
        }

        [HttpPost("/Clinic/MealPlans/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ClinicMealPlanInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.MealPlans
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (plan == null)
            {
                return NotFound();
            }

            var access = await ResolvePatientAccessAsync(plan.UserId, input.ClinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return NotFound();
            }

            var normalizedPlanJson = CleanJson(input.PlanJson);
            if (!IsValidPlanJson(normalizedPlanJson))
            {
                TempData["Error"] = _t.Get("clinic.mealPlans.invalidJson");
                input.PlanJson = normalizedPlanJson;
                return View(BuildEditorModel(access, summary, input, isNew: false, isLatestAssigned: false, id));
            }

            plan.Title = NormalizeTitle(input.Title, summary);
            plan.PlanType = NormalizePlanType(input.PlanType);
            plan.DietType = string.IsNullOrWhiteSpace(input.DietType) ? summary.DietSlug : input.DietType.Trim();
            plan.CalorieTarget = input.CalorieTarget > 0 ? input.CalorieTarget : summary.CalorieTarget ?? 2000;
            plan.PlanJson = normalizedPlanJson;

            await _db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = _t.Get("clinic.mealPlans.saved");

            return RedirectToAction(
                nameof(Details),
                new { id = plan.Id, clinicId = access.ClinicId });
        }

        [HttpGet("/Clinic/MealPlans/Details/{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.MealPlans.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (plan == null)
            {
                return NotFound();
            }

            var access = await ResolvePatientAccessAsync(plan.UserId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return NotFound();
            }

            var latestPlanId = await GetLatestPlanIdAsync(access.PatientId, cancellationToken);
            var item = BuildPlanListItem(
                plan,
                access.ClinicId,
                summary.FullName,
                summary.Email,
                latestPlanId == plan.Id);

            return View(new ClinicMealPlanDetailsViewModel
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = summary,
                Plan = item,
                PlanJson = plan.PlanJson,
                Days = ParsePlanDays(plan.PlanJson)
            });
        }

        [HttpPost("/Clinic/MealPlans/Assign/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            int id,
            int clinicId,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.MealPlans.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (plan == null)
            {
                return NotFound();
            }

            var access = await ResolvePatientAccessAsync(plan.UserId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var assignedPlan = new MealPlan
            {
                UserId = plan.UserId,
                PlanType = NormalizePlanType(plan.PlanType),
                PlanJson = plan.PlanJson,
                EditHistoryJson = plan.EditHistoryJson,
                Title = plan.Title,
                DietType = plan.DietType,
                CalorieTarget = plan.CalorieTarget,
                CreatedAt = DateTime.UtcNow
            };

            _db.MealPlans.Add(assignedPlan);
            await _db.SaveChangesAsync(cancellationToken);

            TempData["Success"] = _t.Get("clinic.mealPlans.assigned");
            return RedirectToAction(
                nameof(Details),
                new { id = assignedPlan.Id, clinicId = access.ClinicId });
        }

        private async Task<int?> ResolveAccessibleClinicIdAsync(int? clinicId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (clinicId.HasValue)
            {
                return await CanAccessClinicAsync(clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolved = await _clinicAccess.ResolveActiveClinicIdAsync(userId, cancellationToken);
            if (!resolved.HasValue)
            {
                return null;
            }

            return await CanAccessClinicAsync(resolved.Value, cancellationToken)
                ? resolved.Value
                : null;
        }

        private async Task<ClinicPatientAccessContext?> ResolvePatientAccessAsync(
            string? patientId,
            int? clinicId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return null;
            }

            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            var resolvedClinicId = await ResolveAccessibleClinicIdForPatientAsync(
                currentUserId,
                patientId,
                clinicId,
                cancellationToken);
            if (!resolvedClinicId.HasValue)
            {
                return null;
            }

            var isAdmin = await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken);
            var isMember = await _clinicAccess.IsClinicMemberAsync(
                currentUserId,
                resolvedClinicId.Value,
                cancellationToken: cancellationToken);
            if (!isAdmin && !isMember)
            {
                return null;
            }

            var patientBelongs = await _clinicAccess.PatientBelongsToClinicAsync(
                patientId,
                resolvedClinicId.Value,
                activeOnly: false,
                requireConsent: false,
                cancellationToken: cancellationToken);
            if (!patientBelongs)
            {
                return null;
            }

            var clinic = await _db.Clinics.AsNoTracking()
                .Where(c => c.Id == resolvedClinicId.Value && c.IsActive)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (clinic == null)
            {
                return null;
            }

            return new ClinicPatientAccessContext(clinic.Id, clinic.Name, patientId);
        }

        private async Task<int?> ResolveAccessibleClinicIdForPatientAsync(
            string currentUserId,
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken)
        {
            if (clinicId.HasValue)
            {
                return await CanAccessClinicAsync(clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolved = await ResolveAccessibleClinicIdAsync(null, cancellationToken);
            if (resolved.HasValue)
            {
                return resolved.Value;
            }

            if (!await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken))
            {
                return null;
            }

            var clinicIds = await _db.ClinicPatients.AsNoTracking()
                .Where(p => p.PatientId == patientId && p.Clinic.IsActive)
                .OrderByDescending(p => p.LinkedAt)
                .Select(p => p.ClinicId)
                .Distinct()
                .Take(2)
                .ToListAsync(cancellationToken);

            return clinicIds.Count == 1 ? clinicIds[0] : null;
        }

        private async Task<bool> CanAccessClinicAsync(int clinicId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId) || clinicId <= 0)
            {
                return false;
            }

            return await _clinicAccess.IsPlatformAdminAsync(userId, cancellationToken) ||
                   await _clinicAccess.IsClinicMemberAsync(userId, clinicId, cancellationToken: cancellationToken);
        }

        private async Task<ClinicMealPlanPatientSummaryViewModel?> BuildPatientSummaryAsync(
            string patientId,
            CancellationToken cancellationToken)
        {
            var patient = await _db.Users.AsNoTracking()
                .Where(u => u.Id == patientId)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    Profile = u.HealthProfile
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (patient == null)
            {
                return null;
            }

            var latestWeight = await _db.WeightLogs.AsNoTracking()
                .Where(w => w.UserId == patientId)
                .OrderByDescending(w => w.Date)
                .Select(w => (double?)w.Weight)
                .FirstOrDefaultAsync(cancellationToken);

            var profileWeight = patient.Profile != null && patient.Profile.Weight > 0
                ? patient.Profile.Weight
                : (double?)null;
            var currentWeight = latestWeight ?? profileWeight;
            var height = patient.Profile != null && patient.Profile.Height > 0
                ? patient.Profile.Height
                : (double?)null;

            return new ClinicMealPlanPatientSummaryViewModel
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                Email = patient.Email,
                Goal = string.IsNullOrWhiteSpace(patient.Profile?.Goal) ? null : patient.Profile.Goal,
                DietSlug = string.IsNullOrWhiteSpace(patient.Profile?.DietSlug) ? null : patient.Profile.DietSlug,
                LatestWeight = currentWeight,
                Bmi = height.HasValue && currentWeight.HasValue
                    ? Math.Round(currentWeight.Value / Math.Pow(height.Value / 100.0, 2), 1)
                    : null,
                CalorieTarget = patient.Profile?.CalorieTarget > 0 ? patient.Profile.CalorieTarget : null,
                WaterGoal = patient.Profile?.WaterGoal > 0 ? patient.Profile.WaterGoal : 8
            };
        }

        private async Task<ClinicMealPlanEditorViewModel?> BuildCreateModelAsync(
            ClinicPatientAccessContext access,
            CancellationToken cancellationToken)
        {
            var summary = await BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (summary == null)
            {
                return null;
            }

            var dietSlug = summary.DietSlug ?? "mediterranean";
            var planJson = _mealPlanService.GenerateWeekPlan(dietSlug);
            var input = new ClinicMealPlanInputViewModel
            {
                ClinicId = access.ClinicId,
                Title = BuildDefaultTitle(summary, "standard"),
                PlanType = "standard",
                DietType = dietSlug,
                CalorieTarget = summary.CalorieTarget ?? 2000,
                PlanJson = planJson
            };

            return BuildEditorModel(access, summary, input, isNew: true, isLatestAssigned: false);
        }

        private ClinicMealPlanEditorViewModel BuildEditorModel(
            ClinicPatientAccessContext access,
            ClinicMealPlanPatientSummaryViewModel summary,
            MealPlan plan,
            bool isLatestAssigned) =>
            new()
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = summary,
                PlanId = plan.Id,
                IsNew = false,
                IsLatestAssigned = isLatestAssigned,
                Title = string.IsNullOrWhiteSpace(plan.Title) ? BuildDefaultTitle(summary, plan.PlanType) : plan.Title,
                PlanType = NormalizePlanType(plan.PlanType),
                DietType = plan.DietType,
                CalorieTarget = plan.CalorieTarget > 0 ? plan.CalorieTarget : summary.CalorieTarget ?? 2000,
                PlanJson = plan.PlanJson,
                Days = ParsePlanDays(plan.PlanJson)
            };

        private ClinicMealPlanEditorViewModel BuildEditorModel(
            ClinicPatientAccessContext access,
            ClinicMealPlanPatientSummaryViewModel summary,
            ClinicMealPlanInputViewModel input,
            bool isNew,
            bool isLatestAssigned,
            int? planId = null) =>
            new()
            {
                ClinicId = access.ClinicId,
                ClinicName = access.ClinicName,
                Patient = summary,
                PlanId = planId,
                IsNew = isNew,
                IsLatestAssigned = isLatestAssigned,
                Title = NormalizeTitle(input.Title, summary),
                PlanType = NormalizePlanType(input.PlanType),
                DietType = string.IsNullOrWhiteSpace(input.DietType) ? summary.DietSlug : input.DietType.Trim(),
                CalorieTarget = input.CalorieTarget > 0 ? input.CalorieTarget : summary.CalorieTarget ?? 2000,
                PlanJson = input.PlanJson,
                Days = ParsePlanDays(input.PlanJson)
            };

        private async Task<PlanSaveResult> BuildPlanForSaveAsync(
            ClinicMealPlanInputViewModel input,
            HealthProfile? profile,
            ClinicMealPlanPatientSummaryViewModel summary,
            CancellationToken cancellationToken)
        {
            var action = input.SubmitAction?.Trim().ToLowerInvariant() ?? "manual";
            var dietSlug = string.IsNullOrWhiteSpace(input.DietType)
                ? summary.DietSlug ?? "mediterranean"
                : input.DietType.Trim();
            var calorieTarget = input.CalorieTarget > 0 ? input.CalorieTarget : summary.CalorieTarget ?? 2000;

            if (action == "generate-standard")
            {
                var standardJson = _mealPlanService.GenerateWeekPlan(dietSlug, DateTime.UtcNow.Millisecond);
                return PlanSaveResult.Ok(
                    standardJson,
                    "standard",
                    BuildDefaultTitle(summary, "standard"),
                    dietSlug,
                    calorieTarget);
            }

            if (action == "generate-ai")
            {
                if (profile != null)
                {
                    try
                    {
                        var aiJson = await _geminiService.GenerateMealPlanAsync(profile, dietSlug, _t.CurrentLang);
                        aiJson = CleanJson(aiJson);
                        if (IsValidPlanJson(aiJson))
                        {
                            return PlanSaveResult.Ok(
                                aiJson,
                                "ai",
                                BuildDefaultTitle(summary, "ai"),
                                dietSlug,
                                calorieTarget);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Clinic AI meal-plan generation failed; using algorithm fallback.");
                    }
                }

                var fallbackJson = _mealPlanService.GenerateWeekPlan(dietSlug, DateTime.UtcNow.Millisecond);
                return PlanSaveResult.Ok(
                    fallbackJson,
                    "standard",
                    BuildDefaultTitle(summary, "standard"),
                    dietSlug,
                    calorieTarget,
                    usedFallback: true);
            }

            var planJson = CleanJson(input.PlanJson);
            if (!IsValidPlanJson(planJson))
            {
                return PlanSaveResult.Fail(_t.Get("clinic.mealPlans.invalidJson"));
            }

            return PlanSaveResult.Ok(
                planJson,
                NormalizePlanType(input.PlanType),
                NormalizeTitle(input.Title, summary),
                dietSlug,
                calorieTarget);
        }

        private async Task<Dictionary<string, int>> GetLatestPlanIdsByPatientAsync(
            IReadOnlyCollection<string> patientIds,
            CancellationToken cancellationToken)
        {
            var latestPlans = await _db.MealPlans.AsNoTracking()
                .Where(m => patientIds.Contains(m.UserId))
                .GroupBy(m => m.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    PlanId = g.OrderByDescending(m => m.CreatedAt).Select(m => m.Id).FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return latestPlans.ToDictionary(p => p.UserId, p => p.PlanId);
        }

        private async Task<int?> GetLatestPlanIdAsync(string patientId, CancellationToken cancellationToken) =>
            await _db.MealPlans.AsNoTracking()
                .Where(m => m.UserId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(cancellationToken);

        private ClinicMealPlanListItemViewModel BuildPlanListItem(
            MealPlan plan,
            int clinicId,
            string patientName,
            string? patientEmail,
            bool isLatestAssigned) =>
            new()
            {
                Id = plan.Id,
                ClinicId = clinicId,
                PatientId = plan.UserId,
                PatientName = patientName,
                PatientEmail = patientEmail,
                Title = string.IsNullOrWhiteSpace(plan.Title)
                    ? _t.Get("clinic.mealPlans.untitledPlan")
                    : plan.Title,
                PlanType = NormalizePlanType(plan.PlanType),
                DietType = plan.DietType,
                CalorieTarget = plan.CalorieTarget,
                CreatedAt = plan.CreatedAt,
                IsLatestAssigned = isLatestAssigned,
                DayCount = ParsePlanDays(plan.PlanJson).Count
            };

        private static List<ClinicMealPlanDayViewModel> ParsePlanDays(string? planJson)
        {
            if (string.IsNullOrWhiteSpace(planJson))
            {
                return [];
            }

            try
            {
                using var doc = JsonDocument.Parse(planJson);
                var root = doc.RootElement;
                var daysElement = root;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("days", out var objectDays))
                {
                    daysElement = objectDays;
                }

                if (daysElement.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var days = new List<ClinicMealPlanDayViewModel>();
                foreach (var dayElement in daysElement.EnumerateArray())
                {
                    var day = new ClinicMealPlanDayViewModel
                    {
                        Day = GetInt(dayElement, "day"),
                        DayNameAr = GetString(dayElement, "dayNameAr"),
                        DayNameEn = GetString(dayElement, "dayNameEn"),
                        TotalCalories = GetDouble(dayElement, "totalCalories"),
                        TotalProtein = GetDouble(dayElement, "totalProtein"),
                        TotalCarbs = GetDouble(dayElement, "totalCarbs"),
                        TotalFats = GetDouble(dayElement, "totalFats")
                    };

                    if (dayElement.TryGetProperty("meals", out var mealsElement) &&
                        mealsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var mealElement in mealsElement.EnumerateArray())
                        {
                            day.Meals.Add(new ClinicMealPlanMealViewModel
                            {
                                Type = GetString(mealElement, "type") ?? string.Empty,
                                NameAr = GetString(mealElement, "nameAr"),
                                NameEn = GetString(mealElement, "nameEn"),
                                Calories = GetDouble(mealElement, "calories"),
                                Protein = GetDouble(mealElement, "protein"),
                                Carbs = GetDouble(mealElement, "carbs"),
                                Fats = GetDouble(mealElement, "fats")
                            });
                        }
                    }

                    days.Add(day);
                }

                return days;
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string CleanJson(string json)
        {
            json = (json ?? string.Empty).Trim();
            if (json.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                json = json[7..];
            }
            else if (json.StartsWith("```", StringComparison.Ordinal))
            {
                json = json[3..];
            }

            if (json.EndsWith("```", StringComparison.Ordinal))
            {
                json = json[..^3];
            }

            return json.Trim();
        }

        private static bool IsValidPlanJson(string planJson)
        {
            if (string.IsNullOrWhiteSpace(planJson))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(planJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    return true;
                }

                return root.ValueKind == JsonValueKind.Object &&
                       root.TryGetProperty("days", out var days) &&
                       days.ValueKind == JsonValueKind.Array;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private string BuildDefaultTitle(ClinicMealPlanPatientSummaryViewModel summary, string planType)
        {
            var label = NormalizePlanType(planType) == "ai"
                ? _t.Get("clinic.mealPlans.aiPlan")
                : _t.Get("clinic.mealPlans.manualPlan");
            return $"{label} - {summary.FullName} - {DateTime.Today:yyyy-MM-dd}";
        }

        private string NormalizeTitle(string? title, ClinicMealPlanPatientSummaryViewModel summary) =>
            string.IsNullOrWhiteSpace(title)
                ? BuildDefaultTitle(summary, "standard")
                : title.Trim();

        private static string NormalizePlanType(string? planType) =>
            string.Equals(planType, "ai", StringComparison.OrdinalIgnoreCase) ? "ai" : "standard";

        private static string? GetString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

        private static int GetInt(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
                ? value
                : 0;

        private static double GetDouble(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value)
                ? value
                : 0;

        private sealed record ClinicPatientAccessContext(int ClinicId, string ClinicName, string PatientId);

        private sealed record PlanSaveResult(
            bool Success,
            string PlanJson,
            string PlanType,
            string Title,
            string? DietType,
            double CalorieTarget,
            bool UsedFallback,
            string? Message)
        {
            public static PlanSaveResult Ok(
                string planJson,
                string planType,
                string title,
                string? dietType,
                double calorieTarget,
                bool usedFallback = false) =>
                new(true, planJson, planType, title, dietType, calorieTarget, usedFallback, null);

            public static PlanSaveResult Fail(string message) =>
                new(false, string.Empty, "standard", string.Empty, null, 0, false, message);
        }
    }
}
