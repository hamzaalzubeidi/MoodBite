using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MoodBite.Models;

namespace MoodBite.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

        private static readonly string[] Models =
        [
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite"
        ];

        private const string VisionModel = "gemini-2.5-flash";

        public GeminiService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<GeminiService> logger,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient          = httpClient;
            _logger              = logger;
            _cache               = cache;
            _httpContextAccessor = httpContextAccessor;
            _apiKey              = config["Gemini:ApiKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(_apiKey))
                _logger.LogError("Gemini API key is missing. Set GEMINI_API_KEY.");
            else
                _logger.LogInformation("GeminiService initialised with configured API key.");
        }

        // ── Rate limit helpers ────────────────────────────────────────────────

        private string? GetCurrentUserId()
        {
            var ctx = _httpContextAccessor.HttpContext;
            return ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        private void EnforceRateLimit()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var key = $"gemini_cooldown_{userId}";
            if (_cache.TryGetValue(key, out _))
                throw new InvalidOperationException("يرجى الانتظار قليلاً قبل الطلب التالي.");

            _cache.Set(key, true, TimeSpan.FromSeconds(30));
        }

        // ── Core HTTP call ────────────────────────────────────────────────────

        private async Task<string> CallGeminiAsync(string prompt, bool forceJson = true)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            object generationConfig = forceJson
                ? (object)new { temperature = 0.7, maxOutputTokens = 8192, responseMimeType = "application/json" }
                : new { temperature = 0.7, maxOutputTokens = 4096 };

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig
            };

            var bodyJson = JsonSerializer.Serialize(requestBody);

            Exception? lastEx = null;
            foreach (var model in Models)
            {
                var url = $"{ApiBase}/{model}:generateContent?key={_apiKey}";
                _logger.LogInformation("Calling Gemini model {Model}", model);

                var content  = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini [{Model}] returned {Status}: {Body}", model, (int)response.StatusCode, errBody[..Math.Min(500, errBody.Length)]);
                    lastEx = new Exception($"Gemini [{model}] {(int)response.StatusCode}: {errBody}");
                    if ((int)response.StatusCode is 503 or 429 or 400 or 404) continue;
                    throw lastEx;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Gemini [{Model}] responded OK ({Bytes} bytes). Raw preview: {Preview}",
                    model, responseJson.Length, responseJson[..Math.Min(500, responseJson.Length)]);

                using var doc = JsonDocument.Parse(responseJson);

                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                foreach (var part in parts.EnumerateArray())
                {
                    bool isThought = part.TryGetProperty("thought", out var t) && t.ValueKind == JsonValueKind.True;
                    if (!isThought && part.TryGetProperty("text", out var textEl))
                        return textEl.GetString() ?? string.Empty;
                }

                return string.Empty;
            }

            throw lastEx ?? new Exception("All Gemini models unavailable.");
        }

        // ── Vision call ───────────────────────────────────────────────────────

        private async Task<string> CallGeminiVisionAsync(byte[] imageBytes, string mimeType, string prompt)
        {
            var base64 = Convert.ToBase64String(imageBytes);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inlineData = new { mimeType, data = base64 } },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 2048, responseMimeType = "application/json" }
            };

            var bodyJson = JsonSerializer.Serialize(requestBody,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            var url     = $"{ApiBase}/{VisionModel}:generateContent?key={_apiKey}";
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini vision [{VisionModel}] {(int)response.StatusCode}: {errBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini vision raw preview: {Preview}", responseJson[..Math.Min(500, responseJson.Length)]);

            using var doc = JsonDocument.Parse(responseJson);
            var parts = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            foreach (var part in parts.EnumerateArray())
            {
                bool isThought = part.TryGetProperty("thought", out var t) && t.ValueKind == JsonValueKind.True;
                if (!isThought && part.TryGetProperty("text", out var textEl))
                    return textEl.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        // ── Food photo analysis ───────────────────────────────────────────────

        public async Task<string> AnalyzeFoodPhotoAsync(byte[] imageBytes, string mimeType, string lang)
        {
            var langNote = lang == "ar"
                ? "Use Arabic for nameAr, descriptionAr, servingSizeAr fields. Use English for nameEn, descriptionEn, servingSize fields."
                : "Use English for nameEn, descriptionEn, servingSize. Provide Arabic for nameAr, descriptionAr, servingSizeAr.";

            var prompt = $@"You are a professional nutritionist and food recognition AI. Analyze the food in this image.

{langNote}

Return ONLY a JSON object with this exact structure, no markdown, no explanation, no code fences:
{{
  ""nameAr"": ""اسم الطعام بالعربية"",
  ""nameEn"": ""Food name in English"",
  ""confidence"": 85,
  ""calories"": 450,
  ""protein"": 25,
  ""carbs"": 45,
  ""fats"": 15,
  ""servingSize"": ""1 portion (~250g)"",
  ""servingSizeAr"": ""حصة واحدة (~250 غ)"",
  ""descriptionEn"": ""Brief description of this food and its health properties"",
  ""descriptionAr"": ""وصف مختصر لهذا الطعام وخصائصه الصحية"",
  ""alternatives"": [
    {{""nameAr"": ""اسم بديل"", ""nameEn"": ""Alternative name"", ""calories"": 400}},
    {{""nameAr"": ""اسم بديل آخر"", ""nameEn"": ""Another alternative"", ""calories"": 380}}
  ]
}}

Rules:
- confidence: integer 0–100
- calories/protein/carbs/fats: estimated values for the visible serving size
- alternatives: 2–3 other foods this might be, if confidence < 90
- If you cannot identify food, set confidence to 0 and nameEn to ""Unknown food""
- All numeric values must be realistic estimates for the visible portion
- Output JSON only — no markdown, no code fences";

            return await CallGeminiVisionAsync(imageBytes, mimeType, prompt);
        }

        // ── Profile array helper ──────────────────────────────────────────────

        private static string FormatProfileArray(string? jsonOrText)
        {
            if (string.IsNullOrWhiteSpace(jsonOrText)) return "none";
            try
            {
                using var doc = JsonDocument.Parse(jsonOrText);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var items = doc.RootElement.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    var result = string.Join(", ", items);
                    return string.IsNullOrWhiteSpace(result) ? "none" : result;
                }
            }
            catch { }
            return jsonOrText;
        }

        // ── Meal plan generation ──────────────────────────────────────────────

        public async Task<string> GenerateMealPlanAsync(HealthProfile profile, string dietSlug, string lang)
        {
            EnforceRateLimit();

            var allergens   = FormatProfileArray(profile.Allergens);
            var conditions  = FormatProfileArray(profile.HealthConditions);
            var preferences = FormatProfileArray(profile.FoodPreferences);
            var langNote    = lang == "ar"
                ? "Use Arabic text for all nameAr fields and English text for all nameEn fields."
                : "Use English text for nameEn fields and provide Arabic equivalents for nameAr fields.";

            var prompt = $@"You are a professional nutritionist. Generate a 7-day personalised meal plan.

User profile:
- Age: {profile.Age}, Gender: {profile.Gender}
- Height: {profile.Height} cm, Weight: {profile.Weight} kg
- Goal: {profile.Goal}
- Activity level: {profile.ActivityLevel}
- Daily calorie target: {profile.CalorieTarget:F0} kcal
- Diet type: {dietSlug}
- Allergens to avoid: {allergens}
- Health conditions: {conditions}
- Food preferences: {preferences}
- Budget: {profile.Budget}
- Cooking style: {profile.CookingStyle}

{langNote}

Return a single JSON object only — no markdown, no code fences, no explanation:
{{
  ""days"": [
    {{
      ""day"": 1,
      ""dayNameAr"": ""الاثنين"",
      ""dayNameEn"": ""Monday"",
      ""totalCalories"": 2000,
      ""totalProtein"": 150,
      ""totalCarbs"": 200,
      ""totalFats"": 70,
      ""meals"": [
        {{
          ""type"": ""breakfast"",
          ""nameAr"": ""اسم الوجبة بالعربية"",
          ""nameEn"": ""Meal name in English"",
          ""calories"": 450,
          ""protein"": 35,
          ""carbs"": 45,
          ""fats"": 18
        }},
        {{""type"":""snack1"",""nameAr"":""....."",""nameEn"":""..."",""calories"":150,""protein"":5,""carbs"":20,""fats"":5}},
        {{""type"":""lunch"",""nameAr"":""....."",""nameEn"":""..."",""calories"":600,""protein"":50,""carbs"":65,""fats"":22}},
        {{""type"":""snack2"",""nameAr"":""....."",""nameEn"":""..."",""calories"":150,""protein"":5,""carbs"":20,""fats"":5}},
        {{""type"":""dinner"",""nameAr"":""....."",""nameEn"":""..."",""calories"":650,""protein"":55,""carbs"":50,""fats"":20}}
      ]
    }}
  ]
}}

Generate all 7 days (Monday through Sunday). Each day must have exactly 5 meals: breakfast, snack1, lunch, snack2, dinner.
Make sure daily calories stay close to {profile.CalorieTarget:F0} kcal. Respect the diet type and avoid allergens.
Output JSON only — no markdown, no code fences.";

            return await CallGeminiAsync(prompt);
        }

        // ── Meal plan editing ─────────────────────────────────────────────────

        public async Task<string> EditMealPlanAsync(string existingPlan, string instruction, string lang, bool skipRateLimit = false)
        {
            if (!skipRateLimit) EnforceRateLimit();

            _logger.LogInformation("EditMealPlanAsync called. Instruction: {Instruction}", instruction);

            var langNote = lang == "ar"
                ? "Keep all nameAr values in Arabic and nameEn values in English."
                : "Keep all nameEn values in English and nameAr values in Arabic.";

            var prompt = $@"You are a professional nutritionist. Edit the following weekly meal plan JSON exactly as the instruction says.

Current plan JSON:
{existingPlan}

User instruction: {instruction}

{langNote}

STRICT RULES — you MUST follow every one:
1. Return ONLY the complete, updated JSON object — no markdown fences, no explanation, no extra text.
2. The output must be the ENTIRE meal plan with ALL 7 days, not just the modified day.
3. Do NOT keep or mention any food item that the instruction asks to remove. Replace it in every day and every meal where it appears.
4. Preserve the exact same JSON schema (keys: days, day, dayNameAr, dayNameEn, totalCalories, totalProtein, totalCarbs, totalFats, meals, type, nameAr, nameEn, calories, protein, carbs, fats).
5. Recalculate totalCalories, totalProtein, totalCarbs, totalFats for every day that was modified.
6. Output valid JSON only — no markdown, no code fences, no comments.";

            _logger.LogInformation("Sending EditMealPlanAsync prompt to Gemini ({Chars} chars)", prompt.Length);
            var result = await CallGeminiAsync(prompt);
            _logger.LogInformation("EditMealPlanAsync received Gemini response ({Chars} chars). Preview: {Preview}",
                result.Length, result[..Math.Min(200, result.Length)]);
            return result;
        }

        // ── Workout plan generation ───────────────────────────────────────────

        public async Task<string> GenerateWorkoutPlanAsync(
            HealthProfile profile,
            string? fitnessLevel    = null,
            string? equipment       = null,
            string? workoutDuration = null,
            string? focusArea       = null,
            int?    daysPerWeek     = null,
            string  lang            = "en")
        {
            EnforceRateLimit();

            var conditions = FormatProfileArray(profile.HealthConditions);

            var prefsBlock = "";
            if (!string.IsNullOrEmpty(fitnessLevel) || !string.IsNullOrEmpty(equipment) ||
                !string.IsNullOrEmpty(workoutDuration) || !string.IsNullOrEmpty(focusArea) ||
                daysPerWeek.HasValue)
            {
                prefsBlock = $@"
Workout preferences:
- Fitness level: {fitnessLevel ?? "intermediate"}
- Available equipment: {equipment ?? "gym"}
- Preferred session duration: {workoutDuration ?? "45 min"}
- Focus area: {focusArea ?? "full body"}
- Training days per week: {daysPerWeek?.ToString() ?? "4"} (remaining days should be rest)";
            }

            var restDays = daysPerWeek.HasValue ? 7 - daysPerWeek.Value : 2;

            var prompt = $@"You are a certified personal trainer. Generate a structured 7-day workout plan.

User profile:
- Age: {profile.Age}, Gender: {profile.Gender}
- Weight: {profile.Weight} kg, Height: {profile.Height} cm
- Goal: {profile.Goal}
- Activity level: {profile.ActivityLevel}
- Health conditions: {conditions}{prefsBlock}

Return a single JSON object only — no markdown, no code fences:
{{
  ""planName"": ""Plan name in English"",
  ""description"": ""Brief plan description"",
  ""frequency"": ""{(daysPerWeek ?? 4)} days/week"",
  ""days"": [
    {{
      ""day"": 1,
      ""dayNameAr"": ""الاثنين"",
      ""dayNameEn"": ""Monday"",
      ""focus"": ""Chest & Triceps"",
      ""focusAr"": ""الصدر والعضلة ثلاثية الرؤوس"",
      ""isRest"": false,
      ""duration"": ""{workoutDuration ?? "45 min"}"",
      ""warmup"": ""5 min light cardio and dynamic stretching"",
      ""exercises"": [
        {{
          ""name"": ""Bench Press"",
          ""nameAr"": ""ضغط الصدر"",
          ""sets"": 3,
          ""reps"": ""10-12"",
          ""rest"": ""60s"",
          ""notes"": ""Keep back flat on bench""
        }}
      ],
      ""cooldown"": ""5 min static stretching""
    }},
    {{
      ""day"": 7,
      ""dayNameAr"": ""الأحد"",
      ""dayNameEn"": ""Sunday"",
      ""focus"": ""Rest"",
      ""focusAr"": ""راحة"",
      ""isRest"": true,
      ""duration"": """",
      ""warmup"": """",
      ""exercises"": [],
      ""cooldown"": """"
    }}
  ]
}}

Generate all 7 days. Include exactly {restDays} rest day(s).
For rest days set isRest to true and leave exercises as an empty array.
Tailor the plan to the user's goal ({profile.Goal}) and activity level ({profile.ActivityLevel}).
{(!string.IsNullOrEmpty(equipment) ? $"Use only exercises suitable for: {equipment}." : "")}
{(!string.IsNullOrEmpty(focusArea) ? $"Primary focus area: {focusArea}." : "")}
Include 4-6 exercises per training day. Add warmup and cooldown for every training day.
Output JSON only — no markdown, no code fences.
{(lang == "ar" ? "Respond entirely in Arabic. All exercise names, instructions, day names, and descriptions must be in Arabic." : "")}";

            return await CallGeminiAsync(prompt);
        }

        // ── Chatbot ───────────────────────────────────────────────────────────

        public async Task<string> ChatAsync(string userMessage, string context, string lang)
        {
            var langInstr = lang == "ar"
                ? "Respond in Arabic only. Be warm, friendly and concise."
                : "Respond in English only. Be warm, friendly and concise.";

            var prompt = $@"You are MoodBite, an AI nutrition and health assistant. {langInstr}

User health context: {context}

User message: {userMessage}

Provide helpful, accurate nutrition and health advice. Keep the response under 150 words. Do not use markdown.";

            return await CallGeminiAsync(prompt, forceJson: false);
        }
    }
}
