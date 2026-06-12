using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MoodBite.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly TranslationService _t;
        private readonly ILogger<ChatController> _logger;

        // Shadda-free keywords — diacritics are stripped from the user message before matching (Bug A fix)
        private static readonly string[] _arKeywords =
            ["ما بدي", "ما أحب", "أكره", "بدون", "احذف", "شيل", "غير", "بدل", "ما عندي رغبة"];

        private static readonly string[] _enKeywords =
            ["don't want", "hate", "dislike", "remove", "delete", "no ", "without", "replace", "change", "swap"];

        public ChatController(
            GeminiService geminiService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            TranslationService t,
            ILogger<ChatController> logger)
        {
            _geminiService = geminiService;
            _userManager = userManager;
            _db = db;
            _t = t;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lang = _t.CurrentLang;
            var foodName = ExtractDislikedFood(request.Message);

            _logger.LogInformation("Chat request from {UserId}. Meal-plan edit intent detected: {HasIntent}",
                user.Id, !string.IsNullOrEmpty(foodName));

            if (!string.IsNullOrEmpty(foodName))
                return await HandleFoodRemoval(user, foodName, lang);

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var context = profile != null
                ? $"User: {user.FullName}, Goal: {profile.Goal}, Diet: {profile.DietSlug}, CalorieTarget: {profile.CalorieTarget}"
                : $"User: {user.FullName}";

            try
            {
                var reply = await _geminiService.ChatAsync(request.Message, context, lang);
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chat AI unavailable for user {UserId}.", user.Id);
                var fallback = lang == "ar"
                    ? "المساعد الذكي غير متاح حالياً. يمكنك متابعة استخدام لوحة التغذية وخطط الوجبات، وسنحاول مرة أخرى لاحقاً."
                    : "The AI assistant is unavailable right now. You can keep using the nutrition dashboard and meal plans, then try again later.";
                return Ok(new { reply = fallback });
            }
        }

        // Strip Arabic diacritics (harakat + shadda U+064B–U+065F) so OrdinalIgnoreCase matching
        // works regardless of whether the user typed tashkeel or not.
        private static string StripArabicDiacritics(string s) =>
            Regex.Replace(s, "[ً-ٟ]", "");

        // Returns the food name when a dislike/removal intent is detected, otherwise null.
        private static string? ExtractDislikedFood(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var normalized = StripArabicDiacritics(message);

            foreach (var kw in _arKeywords)
            {
                var idx = normalized.IndexOf(kw, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                // Extract food name from the ORIGINAL message at the same character offset
                var after = message[(idx + kw.Length)..].Trim();
                // Stop at Arabic prepositions / conjunctions
                after = Regex.Split(after, @"\s+(من|في|لأن|لان|عشان|ولا|و)\s+")[0].Trim();
                if (after.Length >= 2) return after;
            }

            foreach (var kw in _enKeywords)
            {
                var idx = message.IndexOf(kw, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var after = message[(idx + kw.Length)..].Trim();
                // Strip leading articles
                after = Regex.Replace(after, @"^(the|a|an|my)\s+", "", RegexOptions.IgnoreCase);
                // Stop at common stop-word phrases
                after = Regex.Split(after, @"\s+(from|in|at|please|because|anymore|ever)\b", RegexOptions.IgnoreCase)[0]
                             .TrimEnd('.', '!', '?', ',')
                             .Trim();
                if (after.Length >= 2) return after;
            }

            return null;
        }

        private async Task<IActionResult> HandleFoodRemoval(ApplicationUser user, string foodName, string lang)
        {
            // Prefer the latest AI plan; fall back to standard
            var mealPlan = await _db.MealPlans
                .Where(m => m.UserId == user.Id && m.PlanType == "ai")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync()
                ?? await _db.MealPlans
                    .Where(m => m.UserId == user.Id && m.PlanType == "standard")
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

            if (mealPlan == null)
            {
                _logger.LogWarning("HandleFoodRemoval: no meal plan found for user {UserId}", user.Id);
                var noplan = lang == "ar"
                    ? "ما عندك خطة غذائية بعد. اذهب لصفحة الجدول الغذائي لإنشاء واحدة."
                    : "You don't have a meal plan yet. Go to the Meal Plan page to create one.";
                return Ok(new { reply = noplan });
            }

            _logger.LogInformation("HandleFoodRemoval: found plan Id={PlanId} Type={PlanType} for user {UserId}",
                mealPlan.Id, mealPlan.PlanType, user.Id);

            var dietSlug = mealPlan.DietType ?? "mediterranean";
            var instruction =
                $"استبدل {foodName} في جميع الأيام وجميع الوجبات (فطور، غداء، عشاء، سناك) ببديل مناسب لنظام {dietSlug}. " +
                $"يجب أن لا يظهر {foodName} في أي مكان في الخطة الجديدة. " +
                $"حافظ على نفس هيكل JSON بدون أي تغيير في المفاتيح أو التنسيق.";

            string updatedJson;
            try
            {
                _logger.LogInformation("HandleFoodRemoval: calling Gemini to remove '{FoodName}'", foodName);
                updatedJson = CleanJson(await _geminiService.EditMealPlanAsync(mealPlan.PlanJson, instruction, lang));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("الانتظار"))
            {
                _logger.LogWarning("HandleFoodRemoval: rate-limited for user {UserId}", user.Id);
                var rateMsg = lang == "ar"
                    ? "يرجى الانتظار 30 ثانية قبل تعديل الخطة مرة أخرى."
                    : "Please wait 30 seconds before editing the plan again.";
                return Ok(new { reply = rateMsg });
            }

            // Validate Gemini returned parseable JSON before saving (Bug D fix)
            if (string.IsNullOrWhiteSpace(updatedJson))
            {
                _logger.LogError("HandleFoodRemoval: Gemini returned empty response for user {UserId}", user.Id);
                var errMsg = lang == "ar"
                    ? "تعذّر تعديل الخطة. حاول مرة أخرى."
                    : "Failed to update the meal plan. Please try again.";
                return Ok(new { reply = errMsg });
            }

            try { JsonDocument.Parse(updatedJson); }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "HandleFoodRemoval: Gemini returned invalid JSON.");
                var errMsg = lang == "ar"
                    ? "تعذّر تعديل الخطة. حاول مرة أخرى."
                    : "Failed to update the meal plan. Please try again.";
                return Ok(new { reply = errMsg });
            }

            // Retry once with a stricter prompt if the food name is still in the JSON.
            // Pass skipRateLimit=true because the rate-limiter was already set by the first call (Bug C fix).
            if (updatedJson.Contains(foodName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("HandleFoodRemoval: '{FoodName}' still present after first Gemini call — retrying with stricter prompt", foodName);
                var stricter = instruction +
                    $" تحذير مهم: يجب أن لا يظهر {foodName} في أي مكان في الخطة — لا في nameAr ولا في nameEn ولا في أي حقل آخر.";
                try
                {
                    var retried = CleanJson(await _geminiService.EditMealPlanAsync(mealPlan.PlanJson, stricter, lang, skipRateLimit: true));
                    if (!string.IsNullOrWhiteSpace(retried))
                    {
                        try
                        {
                            JsonDocument.Parse(retried);
                            updatedJson = retried;
                            _logger.LogInformation("HandleFoodRemoval: retry succeeded");
                        }
                        catch (JsonException) { /* keep first result — at least it's valid JSON */ }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "HandleFoodRemoval: retry call failed — keeping first result");
                }
            }

            _logger.LogInformation("HandleFoodRemoval: saving updated plan Id={PlanId} to DB", mealPlan.Id);
            await SaveUpdatedPlanAsync(mealPlan, user.Id, instruction, updatedJson);
            _logger.LogInformation("HandleFoodRemoval: plan saved successfully for user {UserId}", user.Id);

            var replyText = lang == "ar"
                ? $"✅ تم إزالة {foodName} من جميع وجباتك وتم استبداله ببدائل مناسبة."
                : $"✅ {foodName} has been removed from all your meals and replaced with suitable alternatives.";

            var buttonText = lang == "ar" ? "عرض الجدول الغذائي المحدث" : "View Updated Meal Plan";

            return Ok(new
            {
                reply = replyText,
                type = "meal_edit",
                showPlanButton = true,
                buttonText,
                buttonUrl = "/MealPlan"
            });
        }

        private async Task SaveUpdatedPlanAsync(MealPlan original, string userId, string instruction, string updatedJson)
        {
            var historyList = new List<object>();
            if (!string.IsNullOrEmpty(original.EditHistoryJson))
            {
                try { historyList = JsonSerializer.Deserialize<List<object>>(original.EditHistoryJson) ?? []; } catch { }
            }
            historyList.Add(new { instruction, timestamp = DateTime.UtcNow, prevPlan = original.PlanJson });

            original.PlanJson = updatedJson;
            original.EditHistoryJson = JsonSerializer.Serialize(historyList);
            original.CreatedAt = DateTime.UtcNow;   // ← السطر المضاف: يخلّي السجل المعدّل هو الأحدث

            _db.MealPlans.Update(original);
            await _db.SaveChangesAsync();
        }

        private static string CleanJson(string json)
        {
            json = json.Trim();
            if (json.StartsWith("```json")) json = json[7..];
            else if (json.StartsWith("```")) json = json[3..];
            if (json.EndsWith("```")) json = json[..^3];
            return json.Trim();
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
