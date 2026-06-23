using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Services;
using MoodBite.ViewModels;

namespace MoodBite.Areas.Clinic.Controllers
{
    [Area("Clinic")]
    [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;

        public AuditLogsController(
            ApplicationDbContext db,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess)
        {
            _db = db;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
        }

        [HttpGet("/Clinic/AuditLogs")]
        public async Task<IActionResult> Index(
            int? clinicId,
            string? action,
            string? q,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            var resolvedClinicId = await ResolveManageableClinicIdAsync(clinicId, cancellationToken);
            if (!resolvedClinicId.HasValue)
            {
                return Forbid();
            }

            var safeTake = Math.Clamp(take, 25, 200);
            var query = _db.AuditLogs.AsNoTracking()
                .Where(log => log.ClinicId == resolvedClinicId.Value);

            if (!string.IsNullOrWhiteSpace(action))
            {
                var actionFilter = action.Trim();
                query = query.Where(log => log.Action.Contains(actionFilter));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var text = q.Trim();
                query = query.Where(log =>
                    log.Summary.Contains(text) ||
                    log.TargetEntityType.Contains(text) ||
                    (log.ActorEmail != null && log.ActorEmail.Contains(text)) ||
                    (log.TargetEntityId != null && log.TargetEntityId.Contains(text)));
            }

            var logs = await query
                .OrderByDescending(log => log.CreatedAtUtc)
                .Take(safeTake)
                .Select(log => new AuditLogListItemViewModel
                {
                    Id = log.Id,
                    CreatedAtUtc = log.CreatedAtUtc,
                    Action = log.Action,
                    Summary = log.Summary,
                    ActorEmail = log.ActorEmail,
                    ActorRoles = log.ActorRoles,
                    ClinicId = log.ClinicId,
                    TargetEntityType = log.TargetEntityType,
                    TargetEntityId = log.TargetEntityId,
                    TargetUserId = log.TargetUserId,
                    IpAddress = log.IpAddress
                })
                .ToListAsync(cancellationToken);

            return View(new AuditLogIndexViewModel
            {
                ClinicId = resolvedClinicId.Value,
                Action = action,
                Query = q,
                Take = safeTake,
                IsClinicScoped = true,
                Logs = logs
            });
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
                return isAdmin || await _clinicAccess.IsClinicOwnerAsync(userId, clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolved = await _clinicAccess.ResolveActiveClinicIdAsync(userId, cancellationToken);
            if (!resolved.HasValue)
            {
                return null;
            }

            return isAdmin || await _clinicAccess.IsClinicOwnerAsync(userId, resolved.Value, cancellationToken)
                ? resolved.Value
                : null;
        }
    }
}

