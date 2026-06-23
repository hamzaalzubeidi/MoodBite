using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using System.Text.Json;

namespace MoodBite.Controllers
{
    [Authorize]
    [EnableRateLimiting("scanner")]
    public class ScannerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiService _geminiService;

        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic" };

        public ScannerController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            GeminiService geminiService)
        {
            _db = db;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _geminiService = geminiService;
        }

        public IActionResult Index() => View();

        // ── Barcode lookup ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Lookup(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return Json(new { success = false, error = "no_barcode" });

            try
            {
                // Use the pre-configured named client (timeout + headers set in Program.cs)
                var client = _httpClientFactory.CreateClient("openfoodfacts");
                var response = await client.GetAsync($"api/v0/product/{Uri.EscapeDataString(barcode)}.json");

                if (!response.IsSuccessStatusCode)
                    return Json(new { success = false, error = $"api_{(int)response.StatusCode}" });

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                // status 0 = not found in OFF database
                if (!doc.RootElement.TryGetProperty("status", out var statusEl) || statusEl.GetInt32() != 1)
                    return Json(new { success = false, error = "not_found" });

                var product = doc.RootElement.GetProperty("product");

                var name   = product.TryGetProperty("product_name",    out var pn)   ? pn.GetString()   : null;
                var nameAr = product.TryGetProperty("product_name_ar", out var pnAr) ? pnAr.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                    name = product.TryGetProperty("product_name_en", out var pnEn) ? pnEn.GetString() : "Unknown";
                if (string.IsNullOrWhiteSpace(nameAr)) nameAr = name;

                double calories = 0, protein = 0, carbs = 0, fats = 0;

                if (product.TryGetProperty("nutriments", out var nut) && nut.ValueKind == JsonValueKind.Object)
                {
                    // Calories: prefer kcal field; fall back to kJ ÷ 4.184
                    if (nut.TryGetProperty("energy-kcal_100g", out var calKcal) && calKcal.ValueKind != JsonValueKind.Null)
                        calories = calKcal.GetDouble();
                    else if (nut.TryGetProperty("energy-kcal", out var calKcal2) && calKcal2.ValueKind != JsonValueKind.Null)
                        calories = calKcal2.GetDouble();
                    else if (nut.TryGetProperty("energy_100g", out var calKj) && calKj.ValueKind != JsonValueKind.Null)
                        calories = calKj.GetDouble() / 4.184;

                    // Protein
                    if (nut.TryGetProperty("proteins_100g", out var prot) && prot.ValueKind != JsonValueKind.Null)
                        protein = prot.GetDouble();
                    else if (nut.TryGetProperty("protein_100g", out var prot2) && prot2.ValueKind != JsonValueKind.Null)
                        protein = prot2.GetDouble();

                    // Carbs
                    if (nut.TryGetProperty("carbohydrates_100g", out var c) && c.ValueKind != JsonValueKind.Null)
                        carbs = c.GetDouble();

                    // Fats
                    if (nut.TryGetProperty("fat_100g", out var f) && f.ValueKind != JsonValueKind.Null)
                        fats = f.GetDouble();
                }

                var imageUrl = product.TryGetProperty("image_url", out var img) ? img.GetString() : null;

                return Json(new
                {
                    success  = true,
                    name,
                    nameAr,
                    calories = Math.Round(calories, 1),
                    protein  = Math.Round(protein,  1),
                    carbs    = Math.Round(carbs,    1),
                    fats     = Math.Round(fats,     1),
                    imageUrl
                });
            }
            catch (TaskCanceledException)
            {
                return Json(new { success = false, error = "timeout" });
            }
            catch
            {
                return Json(new { success = false, error = "service_unavailable" });
            }
        }

        // ── AI photo analysis ─────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> AnalyzePhoto(IFormFile? photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (photo == null || photo.Length == 0)
                return Json(new { success = false, error = "no_file" });

            var mime = photo.ContentType?.ToLower() ?? "";
            if (!AllowedImageTypes.Contains(mime))
                return Json(new { success = false, error = "invalid_type" });

            if (photo.Length > 8 * 1024 * 1024)
                return Json(new { success = false, error = "too_large" });

            // Save image
            var userFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "food-scans", user.Id);
            Directory.CreateDirectory(userFolder);
            var ext      = Path.GetExtension(photo.FileName).ToLower();
            if (string.IsNullOrEmpty(ext)) ext = mime.Contains("png") ? ".png" : ".jpg";
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
            var filePath = Path.Combine(userFolder, fileName);

            byte[] imageBytes;
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }
            imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Call Gemini Vision
            string analysisJson;
            try
            {
                var lang = user.PreferredLanguage ?? "ar";
                analysisJson = await _geminiService.AnalyzeFoodPhotoAsync(imageBytes, mime, lang);
            }
            catch
            {
                return Json(new { success = false, error = "ai_unavailable" });
            }

            // Parse result
            FoodScanResult result;
            try
            {
                var clean = analysisJson.Trim();
                if (clean.StartsWith("```")) clean = string.Join('\n', clean.Split('\n').Skip(1).SkipLast(1));
                result = JsonSerializer.Deserialize<FoodScanResult>(clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("null result");
            }
            catch
            {
                return Json(new { success = false, error = "parse_error" });
            }

            // Persist scan
            var relativePath = $"/uploads/food-scans/{user.Id}/{fileName}";
            var scan = new FoodScan
            {
                UserId          = user.Id,
                ImagePath       = relativePath,
                FoodNameAr      = result.NameAr ?? "",
                FoodNameEn      = result.NameEn ?? "",
                Confidence      = Math.Clamp(result.Confidence, 0, 100),
                Calories        = Math.Max(0, result.Calories),
                Protein         = Math.Max(0, result.Protein),
                Carbs           = Math.Max(0, result.Carbs),
                Fats            = Math.Max(0, result.Fats),
                ServingSize     = result.ServingSize ?? "",
                ServingSizeAr   = result.ServingSizeAr ?? "",
                DescriptionEn   = result.DescriptionEn,
                DescriptionAr   = result.DescriptionAr,
                AlternativesJson = result.Alternatives != null
                    ? JsonSerializer.Serialize(result.Alternatives)
                    : null,
                ScannedAt       = DateTime.UtcNow
            };
            _db.FoodScans.Add(scan);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success      = true,
                scanId       = scan.Id,
                nameAr       = scan.FoodNameAr,
                nameEn       = scan.FoodNameEn,
                confidence   = scan.Confidence,
                calories     = scan.Calories,
                protein      = scan.Protein,
                carbs        = scan.Carbs,
                fats         = scan.Fats,
                servingSize  = scan.ServingSize,
                servingSizeAr= scan.ServingSizeAr,
                descriptionAr= scan.DescriptionAr,
                descriptionEn= scan.DescriptionEn,
                imagePath    = scan.ImagePath,
                alternatives = result.Alternatives
            });
        }

        // ── Log scan to dashboard ────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogScan(int scanId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var scan = await _db.FoodScans.FirstOrDefaultAsync(f => f.Id == scanId && f.UserId == user.Id);
            if (scan == null) return Json(new { success = false, error = "not_found" });

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var target  = profile?.CalorieTarget ?? 2000;
            var today   = DateTime.Today;

            var log = await _db.DayLogs.FirstOrDefaultAsync(d => d.UserId == user.Id && d.Date.Date == today);
            if (log != null)
            {
                log.CaloriesConsumed += scan.Calories;
                log.Protein          += scan.Protein;
                log.Carbs            += scan.Carbs;
                log.Fats             += scan.Fats;
                log.Adherent          = Math.Abs(log.CaloriesConsumed - target) / target <= 0.15;
            }
            else
            {
                _db.DayLogs.Add(new DayLog
                {
                    UserId           = user.Id,
                    Date             = today,
                    CaloriesConsumed = scan.Calories,
                    Protein          = scan.Protein,
                    Carbs            = scan.Carbs,
                    Fats             = scan.Fats,
                    Adherent         = Math.Abs(scan.Calories - target) / target <= 0.15
                });
            }

            scan.LoggedToDashboard = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── Barcode → dashboard (kept for existing barcode form) ─────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToDashboard(double calories, double protein, double carbs, double fats)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var target  = profile?.CalorieTarget ?? 2000;
            var today   = DateTime.Today;

            var log = await _db.DayLogs.FirstOrDefaultAsync(d => d.UserId == user.Id && d.Date.Date == today);
            if (log != null)
            {
                log.CaloriesConsumed += calories;
                log.Protein          += protein;
                log.Carbs            += carbs;
                log.Fats             += fats;
                log.Adherent          = Math.Abs(log.CaloriesConsumed - target) / target <= 0.15;
            }
            else
            {
                _db.DayLogs.Add(new DayLog
                {
                    UserId = user.Id, Date = today,
                    CaloriesConsumed = calories, Protein = protein, Carbs = carbs, Fats = fats,
                    Adherent = Math.Abs(calories - target) / target <= 0.15
                });
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── Scan history ─────────────────────────────────────────────────────

        public async Task<IActionResult> MyScanHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var scans = await _db.FoodScans
                .Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.ScannedAt)
                .ToListAsync();

            return View(scans);
        }
    }

    // DTO for Gemini response deserialization
    internal class FoodScanResult
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public int Confidence { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public string? ServingSize { get; set; }
        public string? ServingSizeAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public List<FoodScanAlternative>? Alternatives { get; set; }
    }

    internal class FoodScanAlternative
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public double Calories { get; set; }
    }
}
