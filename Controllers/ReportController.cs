using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using System.Text;

namespace MoodBite.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ReportService _reportService;
        private readonly TranslationService _t;

        public ReportController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ReportService reportService,
            TranslationService t)
        {
            _db = db;
            _userManager = userManager;
            _reportService = reportService;
            _t = t;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var waterGoal = profile?.WaterGoal > 0 ? profile.WaterGoal : 8;
            var report = await _reportService.GetWeeklyReportAsync(user.Id, profile?.CalorieTarget ?? 2000, waterGoal);

            ViewBag.Profile = profile;
            return View(report);
        }

        public async Task<IActionResult> ExportPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profile = await _db.HealthProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            var waterGoal = profile?.WaterGoal > 0 ? profile.WaterGoal : 8;
            var report = await _reportService.GetWeeklyReportAsync(user.Id, profile?.CalorieTarget ?? 2000, waterGoal);

            // Generate simple HTML report and return as file
            var topMood = string.IsNullOrWhiteSpace(report.TopMood)
                ? ""
                : _t.Get("mood." + report.TopMood);
            var html = GenerateReportHtml(user!, report, topMood);
            var bytes = Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"moodbite-report-{DateTime.Today:yyyy-MM-dd}.html");
        }

        private static string GenerateReportHtml(ApplicationUser user, ReportData report, string topMood)
        {
            var trend = (report.Trend >= 0 ? "+" : "") + report.Trend + "%";
            return string.Concat(
                "<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'><title>تقرير موودبايت</title>",
                "<style>body{font-family:Cairo,sans-serif;padding:40px;background:#fafaf9;color:#1c1917}",
                "h1{color:#059669}table{width:100%;border-collapse:collapse}th,td{padding:12px;border:1px solid #e7e5e4;text-align:right}",
                "th{background:#f5f5f4;font-weight:700}</style></head><body>",
                "<h1>تقرير صحتك الأسبوعي — MoodBite</h1>",
                $"<p><strong>المستخدم:</strong> {user.FullName}</p>",
                $"<p><strong>التاريخ:</strong> {DateTime.Today:yyyy-MM-dd}</p><hr/>",
                "<table><tr><th>المؤشر</th><th>القيمة</th></tr>",
                $"<tr><td>نسبة الالتزام</td><td>{report.AdherencePercent}%</td></tr>",
                $"<tr><td>متوسط السعرات</td><td>{report.AvgCalories} كسع</td></tr>",
                $"<tr><td>هدف السعرات</td><td>{report.CalorieTarget} كسع</td></tr>",
                $"<tr><td>أيام مسجلة</td><td>{report.DaysLogged}</td></tr>",
                $"<tr><td>الاتجاه</td><td>{trend}</td></tr>",
                $"<tr><td>الحالة المزاجية الأكثر</td><td>{topMood}</td></tr>",
                "</table><br/><p style='color:#a8a29e;font-size:12px'>تم التوليد بواسطة موودبايت — MoodBite.com</p>",
                "</body></html>"
            );
        }
    }
}
