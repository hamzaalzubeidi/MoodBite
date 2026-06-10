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
    public class ClinicDashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;
        private readonly ILogger<ClinicDashboardController> _logger;

        public ClinicDashboardController(
            ApplicationDbContext db,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess,
            ILogger<ClinicDashboardController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
            _logger = logger;
        }

        [HttpGet("/Clinic")]
        [HttpGet("/Clinic/ClinicDashboard")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var model = new ClinicDashboardShellViewModel
            {
                IsPlatformAdmin = await _currentUser.IsCurrentUserPlatformAdminAsync(cancellationToken)
            };

            try
            {
                var clinicId = await _clinicAccess.ResolveActiveClinicIdAsync(userId, cancellationToken);
                if (!clinicId.HasValue)
                {
                    return View(model);
                }

                var clinic = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == clinicId.Value && c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return View(model);
                }

                model.HasActiveClinicContext = true;
                model.ClinicId = clinic.Id;
                model.ClinicName = clinic.Name;

                model.ActivePatients = await _db.ClinicPatients.AsNoTracking()
                    .CountAsync(p => p.ClinicId == clinic.Id && p.Status == "active", cancellationToken);

                model.PatientsNeedingFollowUp = await _db.ClinicPatients.AsNoTracking()
                    .CountAsync(p => p.ClinicId == clinic.Id &&
                                     p.Status == "active" &&
                                     p.PrimaryDietitianId == null, cancellationToken);

                model.UpcomingAppointments = await _db.Appointments.AsNoTracking()
                    .CountAsync(a => a.ClinicId == clinic.Id &&
                                     a.Status == "scheduled" &&
                                     a.StartsAt >= DateTime.UtcNow, cancellationToken);
            }
            catch (DbException ex)
            {
                model.ClinicDataUnavailable = true;
                _logger.LogWarning(ex, "Clinic dashboard data is unavailable. The clinic migration may be pending.");
            }

            return View(model);
        }
    }
}
