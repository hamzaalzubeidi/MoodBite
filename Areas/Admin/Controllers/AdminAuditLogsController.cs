using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.ViewModels;

namespace MoodBite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminAuditLogsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminAuditLogsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? clinicId,
            string? action,
            string? q,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 25, 250);
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (clinicId.HasValue)
            {
                query = query.Where(log => log.ClinicId == clinicId.Value);
            }

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
                ClinicId = clinicId,
                Action = action,
                Query = q,
                Take = safeTake,
                Logs = logs
            });
        }
    }
}
