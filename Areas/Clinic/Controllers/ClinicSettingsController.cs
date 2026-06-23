using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Services;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Areas.Clinic.Controllers
{
    [Area("Clinic")]
    [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
    public class ClinicSettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;
        private readonly TranslationService _t;
        private readonly ILogger<ClinicSettingsController> _logger;
        private readonly IAuditLogService _audit;

        public ClinicSettingsController(
            ApplicationDbContext db,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess,
            TranslationService t,
            ILogger<ClinicSettingsController> logger,
            IAuditLogService audit)
        {
            _db = db;
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

                var canEditActiveStatus = _currentUser.Principal?.IsInRole(ApplicationRoles.Admin) == true;
                var clinic = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == resolvedClinicId.Value)
                    .Select(c => new ClinicSettingsViewModel
                    {
                        HasClinicContext = true,
                        CanEditActiveStatus = canEditActiveStatus,
                        ClinicId = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        LegalName = c.LegalName,
                        Email = c.Email,
                        Phone = c.Phone,
                        Country = c.Country,
                        City = c.City,
                        Address = c.Address,
                        IsActive = c.IsActive
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return View(clinic ?? new ClinicSettingsViewModel());
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Clinic settings data is unavailable. The clinic migration may be pending.");
                return View(new ClinicSettingsViewModel { ClinicDataUnavailable = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ClinicSettingsViewModel model, CancellationToken cancellationToken)
        {
            if (model.ClinicId <= 0 || string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = _t.Get("clinic.validation.nameRequired");
                return RedirectToAction(nameof(Index), new { clinicId = model.ClinicId });
            }

            try
            {
                var resolvedClinicId = await ResolveManageableClinicIdAsync(model.ClinicId, cancellationToken);
                if (resolvedClinicId != model.ClinicId)
                {
                    return Forbid();
                }

                var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == model.ClinicId, cancellationToken);
                if (clinic == null)
                {
                    TempData["Error"] = _t.Get("clinic.notFound");
                    return RedirectToAction(nameof(Index));
                }

                clinic.Name = model.Name.Trim();
                clinic.LegalName = Clean(model.LegalName);
                clinic.Email = Clean(model.Email);
                clinic.Phone = Clean(model.Phone);
                clinic.Country = Clean(model.Country);
                clinic.City = Clean(model.City);
                clinic.Address = Clean(model.Address);
                clinic.UpdatedAt = DateTime.UtcNow;

                if (_currentUser.Principal?.IsInRole(ApplicationRoles.Admin) == true)
                {
                    clinic.IsActive = model.IsActive;
                }

                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    "clinic.settings.updated",
                    "Clinic",
                    clinic.Id.ToString(),
                    clinic.Id,
                    summary: "Clinic settings updated.",
                    metadata: new { clinic.IsActive },
                    cancellationToken: cancellationToken);
                TempData["Success"] = _t.Get("clinic.settings.updated");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to update clinic settings. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToAction(nameof(Index), new { clinicId = model.ClinicId });
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

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
