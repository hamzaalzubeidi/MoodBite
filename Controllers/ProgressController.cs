using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace MoodBite.Controllers
{
    [Authorize]
    public class ProgressController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TranslationService _t;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/jpg", "image/png", "image/webp" };

        public ProgressController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entries = await _db.BodyProgressEntries
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.Date)
                .ToListAsync();

            return View(entries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> Add(
            DateTime date,
            double? weight, double? waist, double? hips, double? chest, double? arms,
            string? notes,
            IFormFile? photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            string? photoPath = null;

            if (photo != null && photo.Length > 0)
            {
                var mime = photo.ContentType?.ToLower() ?? "";
                if (photo.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = _t.Get("profile.unsupportedFile");
                    return RedirectToAction("Index");
                }

                if (AllowedTypes.Contains(mime))
                {
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "progress", user.Id);
                    Directory.CreateDirectory(folder);

                    var uploadDate = date == default ? DateTime.Today : date;
                    var fileName = $"{uploadDate:yyyyMMdd}_{Guid.NewGuid():N}.jpg";
                    var filePath = Path.Combine(folder, fileName);

                    using var inputStream = photo.OpenReadStream();
                    Image image;
                    try
                    {
                        image = await Image.LoadAsync(inputStream);
                    }
                    catch (Exception ex) when (ex is UnknownImageFormatException || ex is InvalidImageContentException)
                    {
                        TempData["Error"] = _t.Get("profile.unsupportedFile");
                        return RedirectToAction("Index");
                    }

                    using (image)
                    {
                        if (image.Width > 800)
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(800, 0),
                                Mode = ResizeMode.Max
                            }));

                        await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 });
                    }
                    photoPath = $"/uploads/progress/{user.Id}/{fileName}";
                }
                else
                {
                    TempData["Error"] = _t.Get("profile.unsupportedFile");
                    return RedirectToAction("Index");
                }
            }

            var entry = new BodyProgress
            {
                UserId = user.Id,
                Date = date == default ? DateTime.Today : date,
                Weight = weight,
                Waist = waist,
                Hips = hips,
                Chest = chest,
                Arms = arms,
                Notes = notes,
                PhotoPath = photoPath,
                CreatedAt = DateTime.UtcNow
            };

            _db.BodyProgressEntries.Add(entry);
            await _db.SaveChangesAsync();

            TempData["Success"] = _t.Get("progress.savedSuccess");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entry = await _db.BodyProgressEntries
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (entry != null)
            {
                if (!string.IsNullOrEmpty(entry.PhotoPath))
                {
                    var physPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                        entry.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physPath))
                        System.IO.File.Delete(physPath);
                }
                _db.BodyProgressEntries.Remove(entry);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = _t.Get("progress.deleted");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entries = await _db.BodyProgressEntries
                .Where(b => b.UserId == user.Id)
                .OrderBy(b => b.Date)
                .ToListAsync();

            var html = BuildProgressHtml(user!, entries);
            var bytes = Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"body-progress-{DateTime.Today:yyyy-MM-dd}.html");
        }

        private static string BuildProgressHtml(ApplicationUser user, List<BodyProgress> entries)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'>");
            sb.Append("<title>تقرير التقدم الجسدي</title>");
            sb.Append("<style>body{font-family:Cairo,sans-serif;padding:40px;background:#fafaf9;color:#1c1917}");
            sb.Append("h1{color:#059669}table{width:100%;border-collapse:collapse;margin-top:20px}");
            sb.Append("th,td{padding:10px 12px;border:1px solid #e7e5e4;text-align:right}");
            sb.Append("th{background:#f0fdf4;font-weight:700;color:#065f46}</style></head><body>");
            sb.Append("<h1>📈 تقرير التقدم الجسدي — MoodBite</h1>");
            sb.Append($"<p><strong>المستخدم:</strong> {user.FullName} &nbsp;|&nbsp; <strong>التاريخ:</strong> {DateTime.Today:yyyy-MM-dd}</p><hr/>");

            if (!entries.Any())
            {
                sb.Append("<p>لا توجد بيانات مسجلة بعد.</p>");
            }
            else
            {
                sb.Append("<table><tr><th>التاريخ</th><th>الوزن (كغ)</th><th>الخصر (سم)</th><th>الأرداف (سم)</th><th>الصدر (سم)</th><th>الذراع (سم)</th><th>ملاحظات</th></tr>");
                foreach (var e in entries)
                {
                    sb.Append($"<tr><td>{e.Date:yyyy-MM-dd}</td>");
                    sb.Append($"<td>{(e.Weight.HasValue ? e.Weight.Value.ToString("0.#") : "-")}</td>");
                    sb.Append($"<td>{(e.Waist.HasValue ? e.Waist.Value.ToString("0.#") : "-")}</td>");
                    sb.Append($"<td>{(e.Hips.HasValue ? e.Hips.Value.ToString("0.#") : "-")}</td>");
                    sb.Append($"<td>{(e.Chest.HasValue ? e.Chest.Value.ToString("0.#") : "-")}</td>");
                    sb.Append($"<td>{(e.Arms.HasValue ? e.Arms.Value.ToString("0.#") : "-")}</td>");
                    sb.Append($"<td>{e.Notes ?? "-"}</td></tr>");
                }
                sb.Append("</table>");
            }

            sb.Append("<br/><p style='color:#a8a29e;font-size:12px'>تم التوليد بواسطة موودبايت — MoodBite</p></body></html>");
            return sb.ToString();
        }
    }
}
