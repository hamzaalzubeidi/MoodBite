using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _audit;

        public AdminUsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAuditLogService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                await _audit.LogAsync(
                    user.IsActive ? "admin.users.activated" : "admin.users.deactivated",
                    "ApplicationUser",
                    user.Id,
                    targetUserId: user.Id,
                    summary: user.IsActive ? "Admin activated user." : "Admin deactivated user.",
                    metadata: new { user.IsActive });
                TempData["Success"] = user.IsActive ? "تم تفعيل الحساب / Account activated" : "تم تعطيل الحساب / Account deactivated";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (!ApplicationRoles.IsAdminAssignable(newRole))
            {
                TempData["Error"] = "الدور غير معروف / Unknown role";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count == 1 && string.Equals(currentRoles[0], newRole, StringComparison.Ordinal))
            {
                TempData["Error"] = "المستخدم لديه هذا الدور بالفعل / User already has this role";
                return RedirectToAction("Index");
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);
            await _audit.LogAsync(
                "admin.users.roleChanged",
                "ApplicationUser",
                user.Id,
                targetUserId: user.Id,
                summary: "Admin changed user role.",
                metadata: new { previousRoles = string.Join(",", currentRoles), newRole });

            TempData["Success"] = $"تم تغيير دور المستخدم إلى {newRole} / Role changed to {newRole}";
            return RedirectToAction("Index");
        }
    }
}
