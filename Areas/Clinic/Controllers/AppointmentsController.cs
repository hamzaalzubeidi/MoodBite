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
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ClinicPatientAccessContextService _accessContext;
        private readonly ClinicAppointmentsService _appointmentsService;
        private readonly TranslationService _t;
        private readonly ILogger<AppointmentsController> _logger;
        private readonly IAuditLogService _audit;

        public AppointmentsController(
            ApplicationDbContext db,
            ClinicPatientAccessContextService accessContext,
            ClinicAppointmentsService appointmentsService,
            TranslationService t,
            ILogger<AppointmentsController> logger,
            IAuditLogService audit)
        {
            _db = db;
            _accessContext = accessContext;
            _appointmentsService = appointmentsService;
            _t = t;
            _logger = logger;
            _audit = audit;
        }

        [HttpGet("/Clinic/Appointments")]
        public async Task<IActionResult> Index(
            int? clinicId,
            string? filter,
            string? patientId,
            CancellationToken cancellationToken = default)
        {
            var model = new ClinicAppointmentsIndexViewModel
            {
                Filter = ClinicAppointmentsService.NormalizeFilter(filter),
                PatientId = patientId
            };

            try
            {
                var clinic = await ResolveClinicAsync(clinicId, cancellationToken);
                if (clinic == null)
                {
                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(patientId))
                {
                    var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinic.Id, cancellationToken);
                    if (access == null)
                    {
                        return Forbid();
                    }
                }

                model = await _appointmentsService.BuildIndexModelAsync(
                    clinic.Id,
                    clinic.Name,
                    filter,
                    patientId,
                    cancellationToken);
            }
            catch (DbException ex)
            {
                model.ClinicDataUnavailable = true;
                _logger.LogWarning(ex, "Unable to load clinic appointments.");
            }

            return View(model);
        }

        [HttpGet("/Clinic/Appointments/Create")]
        public async Task<IActionResult> Create(
            int? clinicId,
            string? patientId,
            CancellationToken cancellationToken = default)
        {
            var clinic = await ResolveClinicAsync(clinicId, cancellationToken);
            if (clinic == null)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinic.Id, cancellationToken);
                if (access == null)
                {
                    return Forbid();
                }
            }

            var patients = await _appointmentsService.GetPatientOptionsAsync(clinic.Id, cancellationToken);
            var input = new ClinicAppointmentInputViewModel
            {
                ClinicId = clinic.Id,
                PatientId = patientId ?? string.Empty,
                StartsAt = DateTime.Now.Date.AddDays(1).AddHours(9)
            };

            return View(_appointmentsService.BuildEditorModel(clinic.Id, clinic.Name, patients, input: input));
        }

        [HttpPost("/Clinic/Appointments/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ClinicAppointmentInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var clinic = await ResolveClinicAsync(input.ClinicId, cancellationToken);
            if (clinic == null)
            {
                return Forbid();
            }

            if (!await CanAccessPatientAsync(input.PatientId, clinic.Id, cancellationToken))
            {
                return Forbid();
            }

            if (input.StartsAt == default)
            {
                input.StartsAt = DateTime.Now.Date.AddDays(1).AddHours(9);
            }

            var appointment = await _appointmentsService.CreateAppointmentAsync(
                clinic.Id,
                input,
                cancellationToken);
            if (appointment == null)
            {
                return Unauthorized();
            }

            TempData["Success"] = _t.Get("clinic.appointments.saved");
            await _audit.LogAsync(
                "clinic.appointments.created",
                "Appointment",
                appointment.Id.ToString(),
                clinic.Id,
                appointment.PatientId,
                "Appointment created.",
                new { appointment.Status, appointment.VisitType, appointment.StartsAt },
                cancellationToken);
            return RedirectToAction(nameof(Details), new { id = appointment.Id, clinicId = clinic.Id });
        }

        [HttpGet("/Clinic/Appointments/Edit/{id:int}")]
        public async Task<IActionResult> Edit(
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var clinic = await ResolveClinicAsync(clinicId, cancellationToken);
            if (clinic == null)
            {
                return Forbid();
            }

            var appointment = await _appointmentsService.GetAppointmentAsync(id, clinic.Id, cancellationToken);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!await CanAccessPatientAsync(appointment.PatientId, clinic.Id, cancellationToken))
            {
                return Forbid();
            }

            var patients = await _appointmentsService.GetPatientOptionsAsync(clinic.Id, cancellationToken);
            return View(_appointmentsService.BuildEditorModel(clinic.Id, clinic.Name, patients, appointment));
        }

        [HttpPost("/Clinic/Appointments/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ClinicAppointmentInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var clinic = await ResolveClinicAsync(input.ClinicId, cancellationToken);
            if (clinic == null)
            {
                return Forbid();
            }

            var appointment = await _appointmentsService.GetAppointmentAsync(id, clinic.Id, cancellationToken);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!await CanAccessPatientAsync(appointment.PatientId, clinic.Id, cancellationToken) ||
                !await CanAccessPatientAsync(input.PatientId, clinic.Id, cancellationToken))
            {
                return Forbid();
            }

            if (input.StartsAt == default)
            {
                input.StartsAt = appointment.StartsAt;
            }

            await _appointmentsService.UpdateAppointmentAsync(appointment, input, cancellationToken);
            await _audit.LogAsync(
                "clinic.appointments.updated",
                "Appointment",
                appointment.Id.ToString(),
                clinic.Id,
                appointment.PatientId,
                "Appointment updated.",
                new { appointment.Status, appointment.VisitType, appointment.StartsAt },
                cancellationToken);
            TempData["Success"] = _t.Get("clinic.appointments.saved");
            return RedirectToAction(nameof(Details), new { id = appointment.Id, clinicId = clinic.Id });
        }

        [HttpGet("/Clinic/Appointments/Details/{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var clinic = await ResolveClinicAsync(clinicId, cancellationToken);
            if (clinic == null)
            {
                return Forbid();
            }

            var appointment = await _appointmentsService.GetAppointmentAsync(id, clinic.Id, cancellationToken);
            if (appointment == null)
            {
                return NotFound();
            }

            if (!await CanAccessPatientAsync(appointment.PatientId, clinic.Id, cancellationToken))
            {
                return Forbid();
            }

            return View(_appointmentsService.BuildDetailsModel(clinic.Id, clinic.Name, appointment));
        }

        private async Task<bool> CanAccessPatientAsync(
            string patientId,
            int clinicId,
            CancellationToken cancellationToken) =>
            await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken) != null;

        private async Task<ClinicSummary?> ResolveClinicAsync(
            int? clinicId,
            CancellationToken cancellationToken)
        {
            var resolvedClinicId = await _accessContext.ResolveAccessibleClinicIdAsync(clinicId, cancellationToken);
            if (!resolvedClinicId.HasValue)
            {
                return null;
            }

            return await _db.Clinics.AsNoTracking()
                .Where(c => c.Id == resolvedClinicId.Value && c.IsActive)
                .Select(c => new ClinicSummary(c.Id, c.Name))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private sealed record ClinicSummary(int Id, string Name);
    }
}
