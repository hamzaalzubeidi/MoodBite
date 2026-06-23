using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class ClinicStaffController : Controller
    {
        private static readonly string[] AssignableStaffRoles =
        [
            ApplicationRoles.Dietitian,
            ApplicationRoles.ClinicStaff
        ];

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;
        private readonly TranslationService _t;
        private readonly ILogger<ClinicStaffController> _logger;
        private readonly IAuditLogService _audit;

        public ClinicStaffController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess,
            TranslationService t,
            ILogger<ClinicStaffController> logger,
            IAuditLogService audit)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
            _t = t;
            _logger = logger;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? clinicId, CancellationToken cancellationToken)
        {
            try
            {
                var resolvedClinicId = await ResolveManageableClinicIdAsync(clinicId, cancellationToken);
                if (!resolvedClinicId.HasValue)
                {
                    return Forbid();
                }

                var clinicName = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == resolvedClinicId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                var members = await _db.ClinicMembers.AsNoTracking()
                    .Where(m => m.ClinicId == resolvedClinicId.Value)
                    .OrderBy(m => m.Role)
                    .ThenBy(m => m.User.FullName)
                    .Select(m => new ClinicStaffMemberViewModel
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        FullName = m.User.FullName,
                        Email = m.User.Email,
                        Role = m.Role,
                        IsActive = m.IsActive,
                        JoinedAt = m.JoinedAt
                    })
                    .ToListAsync(cancellationToken);

                return View(new ClinicStaffIndexViewModel
                {
                    HasClinicContext = true,
                    ClinicId = resolvedClinicId.Value,
                    ClinicName = clinicName,
                    Members = members
                });
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Clinic staff data is unavailable. The clinic migration may be pending.");
                return View(new ClinicStaffIndexViewModel { ClinicDataUnavailable = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(ClinicAddStaffMemberViewModel model, CancellationToken cancellationToken)
        {
            if (model.ClinicId <= 0 ||
                string.IsNullOrWhiteSpace(model.Email) ||
                !AssignableStaffRoles.Contains(model.Role, StringComparer.Ordinal))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToAction(nameof(Index), new { clinicId = model.ClinicId });
            }

            try
            {
                var resolvedClinicId = await ResolveManageableClinicIdAsync(model.ClinicId, cancellationToken);
                if (resolvedClinicId != model.ClinicId)
                {
                    return Forbid();
                }

                var user = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user == null)
                {
                    TempData["Error"] = _t.Get("clinic.userNotFound");
                    return RedirectToAction(nameof(Index), new { clinicId = model.ClinicId });
                }

                var currentUserId = _currentUser.GetCurrentUserId();
                var member = await _db.ClinicMembers
                    .FirstOrDefaultAsync(m => m.ClinicId == model.ClinicId && m.UserId == user.Id, cancellationToken);

                var isNewMember = member == null;
                if (member == null)
                {
                    _db.ClinicMembers.Add(new ClinicMember
                    {
                        ClinicId = model.ClinicId,
                        UserId = user.Id,
                        Role = model.Role,
                        IsActive = true,
                        InvitedByUserId = currentUserId
                    });
                }
                else
                {
                    member.Role = model.Role;
                    member.IsActive = true;
                }

                await EnsureIdentityRoleAsync(user, model.Role);
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    isNewMember ? "clinic.staff.added" : "clinic.staff.updated",
                    "ClinicMember",
                    member?.Id.ToString(),
                    model.ClinicId,
                    user.Id,
                    isNewMember ? "Clinic staff member added." : "Clinic staff member updated/reactivated.",
                    new { role = model.Role, isActive = true },
                    cancellationToken);

                TempData["Success"] = _t.Get("clinic.staff.added");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to add clinic staff member. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToAction(nameof(Index), new { clinicId = model.ClinicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMember(int id, int clinicId, CancellationToken cancellationToken)
        {
            try
            {
                var resolvedClinicId = await ResolveManageableClinicIdAsync(clinicId, cancellationToken);
                if (resolvedClinicId != clinicId)
                {
                    return Forbid();
                }

                var member = await _db.ClinicMembers
                    .FirstOrDefaultAsync(m => m.Id == id && m.ClinicId == clinicId, cancellationToken);
                var isNewMember = member == null;
                if (member == null)
                {
                    TempData["Error"] = _t.Get("clinic.memberNotFound");
                    return RedirectToAction(nameof(Index), new { clinicId });
                }

                if (member.Role == ApplicationRoles.ClinicOwner && member.IsActive)
                {
                    var activeOwners = await _db.ClinicMembers.CountAsync(m =>
                        m.ClinicId == clinicId &&
                        m.Role == ApplicationRoles.ClinicOwner &&
                        m.IsActive,
                        cancellationToken);

                    if (activeOwners <= 1)
                    {
                        TempData["Error"] = _t.Get("clinic.staff.cannotDeactivateLastOwner");
                        return RedirectToAction(nameof(Index), new { clinicId });
                    }
                }

                member.IsActive = !member.IsActive;
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    member.IsActive ? "clinic.staff.activated" : "clinic.staff.deactivated",
                    "ClinicMember",
                    member.Id.ToString(),
                    clinicId,
                    member.UserId,
                    member.IsActive ? "Clinic staff member activated." : "Clinic staff member deactivated.",
                    new { member.Role, member.IsActive },
                    cancellationToken);

                TempData["Success"] = member.IsActive
                    ? _t.Get("clinic.staff.activated")
                    : _t.Get("clinic.staff.deactivated");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to toggle clinic member. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToAction(nameof(Index), new { clinicId });
        }

        private async Task<int?> ResolveManageableClinicIdAsync(int? clinicId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var isAdmin = await _currentUser.IsUserPlatformAdminAsync(userId, cancellationToken);
            if (clinicId.HasValue)
            {
                if (isAdmin || await _clinicAccess.IsClinicOwnerAsync(userId, clinicId.Value, cancellationToken))
                {
                    return clinicId.Value;
                }

                return null;
            }

            var resolved = await _clinicAccess.ResolveActiveClinicIdAsync(userId, cancellationToken);
            if (resolved.HasValue && (isAdmin || await _clinicAccess.IsClinicOwnerAsync(userId, resolved.Value, cancellationToken)))
            {
                return resolved;
            }

            return null;
        }

        private async Task EnsureIdentityRoleAsync(ApplicationUser user, string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
