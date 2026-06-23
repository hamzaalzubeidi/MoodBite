using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoodBite.Constants;
using MoodBite.Services;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Areas.Clinic.Controllers
{
    [Area("Clinic")]
    [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
    public class NotesController : Controller
    {
        private readonly ClinicPatientAccessContextService _accessContext;
        private readonly ClinicNotesService _notesService;
        private readonly TranslationService _t;
        private readonly ILogger<NotesController> _logger;
        private readonly IAuditLogService _audit;

        public NotesController(
            ClinicPatientAccessContextService accessContext,
            ClinicNotesService notesService,
            TranslationService t,
            ILogger<NotesController> logger,
            IAuditLogService audit)
        {
            _accessContext = accessContext;
            _notesService = notesService;
            _t = t;
            _logger = logger;
            _audit = audit;
        }

        [HttpGet("/Clinic/Patients/{patientId}/Notes")]
        public async Task<IActionResult> Index(
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (patient == null)
            {
                return NotFound();
            }

            try
            {
                return View(new ClinicNotesIndexViewModel
                {
                    ClinicId = access.ClinicId,
                    ClinicName = access.ClinicName,
                    Patient = patient,
                    Notes = await _notesService.GetPatientNotesAsync(
                        access.ClinicId,
                        access.PatientId,
                        cancellationToken: cancellationToken)
                });
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to load clinical notes.");
                return NotFound();
            }
        }

        [HttpGet("/Clinic/Patients/{patientId}/Notes/Create")]
        public async Task<IActionResult> Create(
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            return patient == null
                ? NotFound()
                : View(_notesService.BuildEditorModel(access, patient));
        }

        [HttpPost("/Clinic/Patients/{patientId}/Notes/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string patientId,
            ClinicNoteInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, input.ClinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            if (patient == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(input.Content))
            {
                TempData["Error"] = _t.Get("clinic.notes.contentRequired");
                return View(_notesService.BuildEditorModel(access, patient, input: input));
            }

            var note = await _notesService.CreateNoteAsync(access.ClinicId, access.PatientId, input, cancellationToken);
            if (note == null)
            {
                return Unauthorized();
            }

            await _audit.LogAsync(
                "clinic.notes.created",
                "ClinicalNote",
                note.Id.ToString(),
                access.ClinicId,
                access.PatientId,
                "Clinical note created.",
                new { note.NoteType, isPrivate = !note.IsSharedWithPatient },
                cancellationToken);
            TempData["Success"] = _t.Get("clinic.notes.saved");

            return RedirectToAction(
                nameof(Details),
                new { patientId = access.PatientId, id = note.Id, clinicId = access.ClinicId });
        }

        [HttpGet("/Clinic/Patients/{patientId}/Notes/Edit/{id:int}")]
        public async Task<IActionResult> Edit(
            string patientId,
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            var note = await _notesService.GetPatientNoteAsync(access.ClinicId, access.PatientId, id, cancellationToken: cancellationToken);
            if (patient == null || note == null)
            {
                return NotFound();
            }

            return View(_notesService.BuildEditorModel(access, patient, note));
        }

        [HttpPost("/Clinic/Patients/{patientId}/Notes/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string patientId,
            int id,
            ClinicNoteInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, input.ClinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            var note = await _notesService.GetPatientNoteAsync(access.ClinicId, access.PatientId, id, cancellationToken: cancellationToken);
            if (patient == null || note == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(input.Content))
            {
                TempData["Error"] = _t.Get("clinic.notes.contentRequired");
                return View(_notesService.BuildEditorModel(access, patient, note, input));
            }

            await _notesService.UpdateNoteAsync(note, input, cancellationToken);
            await _audit.LogAsync(
                "clinic.notes.updated",
                "ClinicalNote",
                note.Id.ToString(),
                access.ClinicId,
                access.PatientId,
                "Clinical note updated.",
                new { note.NoteType },
                cancellationToken);

            TempData["Success"] = _t.Get("clinic.notes.saved");

            return RedirectToAction(
                nameof(Details),
                new { patientId = access.PatientId, id = note.Id, clinicId = access.ClinicId });
        }

        [HttpGet("/Clinic/Patients/{patientId}/Notes/Details/{id:int}")]
        public async Task<IActionResult> Details(
            string patientId,
            int id,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var patient = await _accessContext.BuildPatientSummaryAsync(access.PatientId, cancellationToken);
            var note = await _notesService.GetPatientNoteAsync(access.ClinicId, access.PatientId, id, cancellationToken: cancellationToken);
            if (patient == null || note == null)
            {
                return NotFound();
            }

            await _audit.LogAsync(
                "clinic.notes.viewed",
                "ClinicalNote",
                note.Id.ToString(),
                access.ClinicId,
                access.PatientId,
                "Clinical note viewed.",
                new { note.NoteType },
                cancellationToken);
            return View(_notesService.BuildDetailsModel(access, patient, note));
        }

        [HttpPost("/Clinic/Patients/{patientId}/Notes/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string patientId,
            int id,
            int clinicId,
            CancellationToken cancellationToken = default)
        {
            var access = await _accessContext.ResolvePatientAccessAsync(patientId, clinicId, cancellationToken);
            if (access == null)
            {
                return Forbid();
            }

            var note = await _notesService.GetPatientNoteAsync(access.ClinicId, access.PatientId, id, cancellationToken: cancellationToken);
            if (note == null)
            {
                return NotFound();
            }

            await _notesService.ArchiveNoteAsync(note, cancellationToken);
            await _audit.LogAsync(
                "clinic.notes.archived",
                "ClinicalNote",
                note.Id.ToString(),
                access.ClinicId,
                access.PatientId,
                "Clinical note archived.",
                new { note.NoteType },
                cancellationToken);
            TempData["Success"] = _t.Get("clinic.notes.deleted");

            return RedirectToAction(
                nameof(Index),
                new { patientId = access.PatientId, clinicId = access.ClinicId });
        }
    }
}
