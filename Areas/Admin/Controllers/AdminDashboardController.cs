using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.ActiveUsers = await _db.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalRecipes = await _db.CommunityRecipes.CountAsync();
            ViewBag.PendingRecipes = await _db.CommunityRecipes.CountAsync(r => !r.IsApproved);
            ViewBag.TotalDiets = await _db.Diets.CountAsync();
            ViewBag.TotalChallenges = await _db.Challenges.CountAsync();
            ViewBag.TotalLogs = await _db.DayLogs.CountAsync();

            var recentUsers = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentUsers = recentUsers;

            return View();
        }
    }
}
