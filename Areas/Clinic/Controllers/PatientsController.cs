using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.Services;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Areas.Clinic.Controllers
{
    [Area("Clinic")]
    public class PatientsController : Controller
    {
        private static readonly string[] PatientStatuses = ["pending", "active", "archived", "discharged"];
        private static readonly string[] ConsentFilters = ["all", "granted", "missing"];

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;
        private readonly ReportService _reportService;
        private readonly ClinicNotesService _notesService;
        private readonly ClinicAppointmentsService _appointmentsService;
        private readonly TranslationService _t;
        private readonly ILogger<PatientsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _audit;

        public PatientsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess,
            ReportService reportService,
            ClinicNotesService notesService,
            ClinicAppointmentsService appointmentsService,
            TranslationService t,
            ILogger<PatientsController> logger,
            IEmailService emailService,
            IAuditLogService audit)
        {
            _db = db;
            _userManager = userManager;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
            _reportService = reportService;
            _notesService = notesService;
            _appointmentsService = appointmentsService;
            _t = t;
            _logger = logger;
            _emailService = emailService;
            _audit = audit;
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
        public async Task<IActionResult> Index(
            int? clinicId,
            string? q,
            string status = "all",
            string consent = "all",
            CancellationToken cancellationToken = default)
        {
            var model = new ClinicPatientsIndexViewModel
            {
                Query = q,
                StatusFilter = NormalizeStatusFilter(status),
                ConsentFilter = NormalizeConsentFilter(consent),
                LastInvitationLink = TempData["InvitationLink"] as string
            };

            try
            {
                var resolvedClinicId = await ResolveAccessibleClinicIdAsync(clinicId, cancellationToken);
                if (!resolvedClinicId.HasValue)
                {
                    return View(model);
                }

                var clinic = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == resolvedClinicId.Value && c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinic == null)
                {
                    return View(model);
                }

                model.HasClinicContext = true;
                model.ClinicId = clinic.Id;
                model.ClinicName = clinic.Name;

                var patients = _db.ClinicPatients.AsNoTracking()
                    .Where(p => p.ClinicId == clinic.Id);

                model.TotalPatients = await patients.CountAsync(cancellationToken);
                model.ActivePatients = await patients.CountAsync(p => p.Status == "active", cancellationToken);
                model.PendingPatients = await patients.CountAsync(p => p.Status == "pending", cancellationToken);

                if (model.StatusFilter != "all")
                {
                    patients = patients.Where(p => p.Status == model.StatusFilter);
                }

                if (model.ConsentFilter == "granted")
                {
                    patients = patients.Where(p => p.ConsentGranted);
                }
                else if (model.ConsentFilter == "missing")
                {
                    patients = patients.Where(p => !p.ConsentGranted);
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var query = q.Trim();
                    patients = patients.Where(p =>
                        p.Patient.FullName.Contains(query) ||
                        (p.Patient.Email != null && p.Patient.Email.Contains(query)));
                }

                model.Patients = await patients
                    .OrderBy(p => p.Patient.FullName)
                    .ThenBy(p => p.Patient.Email)
                    .Select(p => new ClinicPatientRosterItemViewModel
                    {
                        Id = p.Id,
                        UserId = p.PatientId,
                        FullName = p.Patient.FullName,
                        Email = p.Patient.Email,
                        Status = p.Status,
                        ConsentGranted = p.ConsentGranted,
                        LinkedAt = p.LinkedAt,
                        PrimaryDietitianName = p.PrimaryDietitian != null ? p.PrimaryDietitian.FullName : null,
                        DietSlug = p.Patient.HealthProfile != null ? p.Patient.HealthProfile.DietSlug : null,
                        Goal = p.Patient.HealthProfile != null ? p.Patient.HealthProfile.Goal : null,
                        Weight = p.Patient.HealthProfile != null ? p.Patient.HealthProfile.Weight : null,
                        CalorieTarget = p.Patient.HealthProfile != null ? p.Patient.HealthProfile.CalorieTarget : null,
                        ProfileUpdatedAt = p.Patient.HealthProfile != null ? p.Patient.HealthProfile.UpdatedAt : null
                    })
                    .Take(100)
                    .ToListAsync(cancellationToken);

                var now = DateTime.UtcNow;
                model.PendingInvitations = await _db.ClinicInvitations.AsNoTracking()
                    .CountAsync(i =>
                        i.ClinicId == clinic.Id &&
                        i.InvitationType == "patient" &&
                        i.Status == "pending" &&
                        i.ExpiresAt >= now,
                        cancellationToken);

                model.Invitations = await _db.ClinicInvitations.AsNoTracking()
                    .Where(i => i.ClinicId == clinic.Id && i.InvitationType == "patient")
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(12)
                    .Select(i => new ClinicPatientInvitationItemViewModel
                    {
                        Id = i.Id,
                        Email = i.Email,
                        Status = i.Status,
                        InvitedByName = i.InvitedBy.FullName,
                        CreatedAt = i.CreatedAt,
                        ExpiresAt = i.ExpiresAt,
                        IsExpired = i.Status == "pending" && i.ExpiresAt < now
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                model.ClinicDataUnavailable = true;
                _logger.LogWarning(ex, "Clinic patients data is unavailable. The clinic migration may be pending.");
            }

            return View(model);
        }

        [HttpGet("/Clinic/Patients/Details/{patientId}")]
        [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
        public async Task<IActionResult> Details(
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return NotFound();
            }

            try
            {
                var resolvedClinicId = await ResolveAccessibleClinicIdAsync(clinicId, cancellationToken);
                if (!resolvedClinicId.HasValue)
                {
                    return Forbid();
                }

                var currentUserId = _currentUser.GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return Unauthorized();
                }

                var isPlatformAdmin = await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken);
                var patientBelongsToClinic = await _clinicAccess.PatientBelongsToClinicAsync(
                    patientId,
                    resolvedClinicId.Value,
                    activeOnly: false,
                    requireConsent: false,
                    cancellationToken: cancellationToken);

                var canAccessPatient = patientBelongsToClinic &&
                    (isPlatformAdmin ||
                     await _clinicAccess.IsClinicMemberAsync(
                         currentUserId,
                         resolvedClinicId.Value,
                         cancellationToken: cancellationToken));

                if (!canAccessPatient)
                {
                    return Forbid();
                }

                var patientLink = await _db.ClinicPatients.AsNoTracking()
                    .Include(p => p.Clinic)
                    .Include(p => p.Patient)
                        .ThenInclude(p => p.HealthProfile)
                    .Include(p => p.PrimaryDietitian)
                    .Where(p => p.ClinicId == resolvedClinicId.Value && p.PatientId == patientId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (patientLink == null)
                {
                    return NotFound();
                }

                var model = BuildPatientDetailsModel(patientLink);

                var weightLogs = await _db.WeightLogs.AsNoTracking()
                    .Where(w => w.UserId == patientId)
                    .OrderByDescending(w => w.Date)
                    .Take(30)
                    .ToListAsync(cancellationToken);

                var mealLogs = await _db.DayLogs.AsNoTracking()
                    .Where(d => d.UserId == patientId)
                    .OrderByDescending(d => d.Date)
                    .Take(14)
                    .ToListAsync(cancellationToken);

                var foodScans = await _db.FoodScans.AsNoTracking()
                    .Where(f => f.UserId == patientId)
                    .OrderByDescending(f => f.ScannedAt)
                    .Take(8)
                    .ToListAsync(cancellationToken);

                var waterLogs = await _db.WaterLogs.AsNoTracking()
                    .Where(w => w.UserId == patientId)
                    .OrderByDescending(w => w.Date)
                    .Take(14)
                    .ToListAsync(cancellationToken);

                var progressEntries = await _db.BodyProgressEntries.AsNoTracking()
                    .Where(p => p.UserId == patientId)
                    .OrderByDescending(p => p.Date)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(cancellationToken);

                var mealPlan = await _db.MealPlans.AsNoTracking()
                    .Where(m => m.UserId == patientId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                model.WeightHistory = BuildWeightHistory(weightLogs);
                model.MealLogs = BuildMealLogs(mealLogs);
                model.FoodScans = BuildFoodScans(foodScans, _t.IsRtl);
                model.WaterLogs = BuildWaterLogs(waterLogs, model.WaterTarget);
                model.ProgressEntries = BuildProgressEntries(progressEntries);
                model.MealPlan = BuildMealPlanSummary(mealPlan);
                model.ClinicalNotes = await _notesService.GetPatientNotesAsync(
                    resolvedClinicId.Value,
                    patientId,
                    take: 5,
                    cancellationToken: cancellationToken);
                model.UpcomingAppointments = await _appointmentsService.GetPatientUpcomingAppointmentsAsync(
                    resolvedClinicId.Value,
                    patientId,
                    take: 5,
                    cancellationToken: cancellationToken);

                ApplyWeightSummary(model, weightLogs, progressEntries);
                ApplyNutritionSummary(model, mealLogs, foodScans);
                ApplyHydrationSummary(model, waterLogs);
                ApplyRecentActivity(model, weightLogs, mealLogs, foodScans, waterLogs, progressEntries, mealPlan);

                model.WeeklyReport = await _reportService.GetWeeklyReportAsync(
                    patientId,
                    model.CalorieTarget ?? 2000,
                    model.WaterTarget);

                await _audit.LogAsync(
                    "clinic.patients.viewed",
                    "ClinicPatient",
                    patientLink.Id.ToString(),
                    patientLink.ClinicId,
                    patientLink.PatientId,
                    "Clinic patient record viewed.",
                    new { patientLink.Status, patientLink.ConsentGranted },
                    cancellationToken);

                return View(model);
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to load clinic patient details. The clinic migration may be pending.");
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
        public async Task<IActionResult> LinkExisting(
            ClinicLinkExistingPatientViewModel model,
            CancellationToken cancellationToken)
        {
            if (model.ClinicId <= 0 || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToPatients(model.ClinicId);
            }

            try
            {
                if (!await CanAccessClinicAsync(model.ClinicId, cancellationToken))
                {
                    return Forbid();
                }

                var user = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user == null)
                {
                    TempData["Error"] = _t.Get("clinic.userNotFound");
                    return RedirectToPatients(model.ClinicId);
                }

                var link = await _db.ClinicPatients
                    .FirstOrDefaultAsync(p => p.ClinicId == model.ClinicId && p.PatientId == user.Id, cancellationToken);

                var isNewLink = link == null;
                if (link == null)
                {
                    link = new ClinicPatient
                    {
                        ClinicId = model.ClinicId,
                        PatientId = user.Id,
                        Status = "active",
                        ConsentGranted = true,
                        ConsentGrantedAt = DateTime.UtcNow
                    };
                    _db.ClinicPatients.Add(link);
                }
                else
                {
                    link.Status = "active";
                    link.ConsentGranted = true;
                    link.ConsentGrantedAt ??= DateTime.UtcNow;
                    link.ArchivedAt = null;
                }

                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    "clinic.patients.linked",
                    "ClinicPatient",
                    link.Id.ToString(),
                    model.ClinicId,
                    user.Id,
                    isNewLink ? "Patient linked to clinic." : "Clinic patient link reactivated.",
                    new { link.Status, link.ConsentGranted },
                    cancellationToken);
                TempData["Success"] = _t.Get("clinic.patients.linked");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to link existing patient. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToPatients(model.ClinicId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        [Authorize(Roles = ApplicationRoles.ClinicAreaAccess)]
        public async Task<IActionResult> Invite(
            ClinicInvitePatientViewModel model,
            CancellationToken cancellationToken)
        {
            if (model.ClinicId <= 0 || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = _t.Get("common.error");
                return RedirectToPatients(model.ClinicId);
            }

            try
            {
                if (!await CanAccessClinicAsync(model.ClinicId, cancellationToken))
                {
                    return Forbid();
                }

                var currentUserId = _currentUser.GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return Unauthorized();
                }

                var email = model.Email.Trim();
                var activeClinic = await _db.Clinics.AsNoTracking()
                    .Where(c => c.Id == model.ClinicId && c.IsActive)
                    .Select(c => new { c.Id, c.Name })
                    .FirstOrDefaultAsync(cancellationToken);
                if (activeClinic == null)
                {
                    TempData["Error"] = _t.Get("clinic.notFound");
                    return RedirectToPatients(model.ClinicId);
                }

                var existingPending = await _db.ClinicInvitations
                    .Where(i =>
                        i.ClinicId == model.ClinicId &&
                        i.Email == email &&
                        i.InvitationType == "patient" &&
                        i.Status == "pending")
                    .ToListAsync(cancellationToken);

                foreach (var invitation in existingPending)
                {
                    invitation.Status = "revoked";
                    invitation.RevokedAt = DateTime.UtcNow;
                }

                var token = GenerateInvitationToken();
                var newInvitation = new ClinicInvitation
                {
                    ClinicId = model.ClinicId,
                    Email = email,
                    TokenHash = HashToken(token),
                    InvitationType = "patient",
                    Status = "pending",
                    InvitedByUserId = currentUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _db.ClinicInvitations.Add(newInvitation);

                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    "clinic.patients.invited",
                    "ClinicInvitation",
                    newInvitation.Id.ToString(),
                    model.ClinicId,
                    summary: "Patient invitation created.",
                    metadata: new { newInvitation.InvitationType, newInvitation.Status },
                    cancellationToken: cancellationToken);

                TempData["Success"] = _t.Get("clinic.invitations.created");
                var invitationLink = Url.Action(
                    nameof(Accept),
                    "Patients",
                    new { area = "Clinic", token },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(invitationLink))
                {
                    var emailResult = await _emailService.SendClinicInvitationAsync(
                        email,
                        activeClinic.Name,
                        invitationLink,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(emailResult.DevelopmentPreviewUrl))
                    {
                        TempData["InvitationLink"] = emailResult.DevelopmentPreviewUrl;
                    }
                    else if (!emailResult.Sent)
                    {
                        TempData["Error"] = _t.Get("clinic.invitations.emailNotConfigured");
                    }
                }
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to invite patient. The clinic migration may be pending.");
                TempData["Error"] = _t.Get("clinic.dataUnavailable");
            }

            return RedirectToPatients(model.ClinicId);
        }

        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Accept(string? token, CancellationToken cancellationToken)
        {
            var model = await BuildAcceptInvitationModelAsync(token, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation(string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                {
                    IsAuthenticated = true,
                    Message = _t.Get("clinic.invitations.invalid")
                });
            }

            try
            {
                var tokenHash = HashToken(token);
                var invitation = await _db.ClinicInvitations
                    .Include(i => i.Clinic)
                    .FirstOrDefaultAsync(i =>
                        i.TokenHash == tokenHash &&
                        i.InvitationType == "patient",
                        cancellationToken);

                if (invitation == null)
                {
                    return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                    {
                        Token = token,
                        IsAuthenticated = true,
                        Message = _t.Get("clinic.invitations.invalid")
                    });
                }

                if (invitation.Status != "pending" || invitation.ExpiresAt < DateTime.UtcNow)
                {
                    if (invitation.Status == "pending" && invitation.ExpiresAt < DateTime.UtcNow)
                    {
                        invitation.Status = "expired";
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                    {
                        Token = token,
                        IsAuthenticated = true,
                        ClinicName = invitation.Clinic.Name,
                        Email = invitation.Email,
                        ExpiresAt = invitation.ExpiresAt,
                        Message = _t.Get("clinic.invitations.expired")
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                    {
                        Token = token,
                        IsAuthenticated = true,
                        IsValid = true,
                        ClinicName = invitation.Clinic.Name,
                        Email = invitation.Email,
                        ExpiresAt = invitation.ExpiresAt,
                        Message = _t.Get("clinic.invitations.emailMismatch")
                    });
                }

                if (!await _clinicAccess.PatientBelongsToClinicAsync(
                        user.Id,
                        invitation.ClinicId,
                        activeOnly: false,
                        cancellationToken: cancellationToken))
                {
                    _db.ClinicPatients.Add(new ClinicPatient
                    {
                        ClinicId = invitation.ClinicId,
                        PatientId = user.Id,
                        Status = "active",
                        ConsentGranted = true,
                        ConsentGrantedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    var patientLink = await _db.ClinicPatients
                        .FirstAsync(p => p.ClinicId == invitation.ClinicId && p.PatientId == user.Id, cancellationToken);

                    patientLink.Status = "active";
                    patientLink.ConsentGranted = true;
                    patientLink.ConsentGrantedAt ??= DateTime.UtcNow;
                    patientLink.ArchivedAt = null;
                }

                invitation.Status = "accepted";
                invitation.AcceptedByUserId = user.Id;
                invitation.AcceptedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(
                    "clinic.patients.invitationAccepted",
                    "ClinicInvitation",
                    invitation.Id.ToString(),
                    invitation.ClinicId,
                    user.Id,
                    "Patient invitation accepted.",
                    new { invitation.Status },
                    cancellationToken);

                return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                {
                    Token = token,
                    IsAuthenticated = true,
                    IsValid = true,
                    IsAccepted = true,
                    ClinicName = invitation.Clinic.Name,
                    Email = invitation.Email,
                    ExpiresAt = invitation.ExpiresAt,
                    Message = _t.Get("clinic.invitations.accepted")
                });
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to accept patient invitation. The clinic migration may be pending.");
                return View(nameof(Accept), new ClinicAcceptInvitationViewModel
                {
                    Token = token,
                    IsAuthenticated = User.Identity?.IsAuthenticated == true,
                    Message = _t.Get("clinic.dataUnavailable")
                });
            }
        }

        private async Task<ClinicAcceptInvitationViewModel> BuildAcceptInvitationModelAsync(
            string? token,
            CancellationToken cancellationToken)
        {
            var model = new ClinicAcceptInvitationViewModel
            {
                Token = token ?? string.Empty,
                IsAuthenticated = User.Identity?.IsAuthenticated == true
            };

            if (string.IsNullOrWhiteSpace(token))
            {
                model.Message = _t.Get("clinic.invitations.invalid");
                return model;
            }

            try
            {
                var tokenHash = HashToken(token);
                var invitation = await _db.ClinicInvitations.AsNoTracking()
                    .Where(i => i.TokenHash == tokenHash && i.InvitationType == "patient")
                    .Select(i => new
                    {
                        i.Email,
                        i.Status,
                        i.ExpiresAt,
                        ClinicName = i.Clinic.Name
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (invitation == null)
                {
                    model.Message = _t.Get("clinic.invitations.invalid");
                    return model;
                }

                model.ClinicName = invitation.ClinicName;
                model.Email = invitation.Email;
                model.ExpiresAt = invitation.ExpiresAt;
                model.IsValid = invitation.Status == "pending" && invitation.ExpiresAt >= DateTime.UtcNow;
                model.Message = model.IsValid
                    ? null
                    : _t.Get("clinic.invitations.expired");
            }
            catch (DbException ex)
            {
                _logger.LogWarning(ex, "Unable to load patient invitation. The clinic migration may be pending.");
                model.Message = _t.Get("clinic.dataUnavailable");
            }

            return model;
        }

        private async Task<int?> ResolveAccessibleClinicIdAsync(int? clinicId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (clinicId.HasValue)
            {
                return await CanAccessClinicAsync(clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolved = await _clinicAccess.ResolveActiveClinicIdAsync(userId, cancellationToken);
            if (!resolved.HasValue)
            {
                return null;
            }

            return await CanAccessClinicAsync(resolved.Value, cancellationToken)
                ? resolved.Value
                : null;
        }

        private async Task<bool> CanAccessClinicAsync(int clinicId, CancellationToken cancellationToken)
        {
            var userId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId) || clinicId <= 0)
            {
                return false;
            }

            return await _clinicAccess.IsPlatformAdminAsync(userId, cancellationToken) ||
                   await _clinicAccess.IsClinicMemberAsync(userId, clinicId, cancellationToken: cancellationToken);
        }

        private RedirectToActionResult RedirectToPatients(int clinicId) =>
            RedirectToAction(nameof(Index), "Patients", new { area = "Clinic", clinicId });

        private static ClinicPatientDetailsViewModel BuildPatientDetailsModel(ClinicPatient patientLink)
        {
            var profile = patientLink.Patient.HealthProfile;
            var height = profile?.Height > 0 ? profile.Height : (double?)null;
            var profileWeight = profile?.Weight > 0 ? profile.Weight : (double?)null;

            return new ClinicPatientDetailsViewModel
            {
                ClinicId = patientLink.ClinicId,
                ClinicName = patientLink.Clinic.Name,
                PatientId = patientLink.PatientId,
                FullName = patientLink.Patient.FullName,
                Email = patientLink.Patient.Email,
                Status = patientLink.Status,
                ConsentGranted = patientLink.ConsentGranted,
                ConsentGrantedAt = patientLink.ConsentGrantedAt,
                LinkedAt = patientLink.LinkedAt,
                PrimaryDietitianName = patientLink.PrimaryDietitian?.FullName,
                Age = profile?.Age > 0 ? profile.Age : null,
                Gender = string.IsNullOrWhiteSpace(profile?.Gender) ? null : profile.Gender,
                Height = height,
                ProfileWeight = profileWeight,
                CurrentWeight = profileWeight,
                Goal = string.IsNullOrWhiteSpace(profile?.Goal) ? null : profile.Goal,
                DietSlug = string.IsNullOrWhiteSpace(profile?.DietSlug) ? null : profile.DietSlug,
                CalorieTarget = profile?.CalorieTarget > 0 ? profile.CalorieTarget : null,
                WaterTarget = profile?.WaterGoal > 0 ? profile.WaterGoal : 8,
                ProfileUpdatedAt = profile?.UpdatedAt
            };
        }

        private static List<ClinicPatientWeightLogItemViewModel> BuildWeightHistory(IReadOnlyList<WeightLog> weightLogs)
        {
            var items = new List<ClinicPatientWeightLogItemViewModel>();

            for (var i = 0; i < weightLogs.Count; i++)
            {
                var log = weightLogs[i];
                var previous = i + 1 < weightLogs.Count ? weightLogs[i + 1] : null;

                items.Add(new ClinicPatientWeightLogItemViewModel
                {
                    Date = log.Date,
                    Weight = log.Weight,
                    ChangeFromPrevious = previous == null
                        ? null
                        : Math.Round(log.Weight - previous.Weight, 1),
                    Note = log.Note
                });
            }

            return items;
        }

        private static List<ClinicPatientDayLogItemViewModel> BuildMealLogs(IEnumerable<DayLog> mealLogs) =>
            mealLogs.Select(log => new ClinicPatientDayLogItemViewModel
            {
                Date = log.Date,
                CaloriesConsumed = log.CaloriesConsumed,
                CaloriesBurned = log.CaloriesBurned,
                Protein = log.Protein,
                Carbs = log.Carbs,
                Fats = log.Fats,
                Mood = log.Mood,
                Adherent = log.Adherent
            }).ToList();

        private static List<ClinicPatientFoodScanItemViewModel> BuildFoodScans(
            IEnumerable<FoodScan> foodScans,
            bool isRtl)
        {
            return foodScans.Select(scan =>
            {
                var name = isRtl
                    ? FirstNonEmpty(scan.FoodNameAr, scan.FoodNameEn)
                    : FirstNonEmpty(scan.FoodNameEn, scan.FoodNameAr);

                var servingSize = isRtl
                    ? FirstNonEmpty(scan.ServingSizeAr, scan.ServingSize)
                    : FirstNonEmpty(scan.ServingSize, scan.ServingSizeAr);

                return new ClinicPatientFoodScanItemViewModel
                {
                    FoodName = name,
                    Confidence = scan.Confidence,
                    Calories = scan.Calories,
                    Protein = scan.Protein,
                    Carbs = scan.Carbs,
                    Fats = scan.Fats,
                    ServingSize = servingSize,
                    LoggedToDashboard = scan.LoggedToDashboard,
                    ScannedAt = scan.ScannedAt
                };
            }).ToList();
        }

        private static List<ClinicPatientWaterLogItemViewModel> BuildWaterLogs(
            IEnumerable<WaterLog> waterLogs,
            int waterTarget) =>
            waterLogs.Select(log => new ClinicPatientWaterLogItemViewModel
            {
                Date = log.Date,
                GlassesCount = log.GlassesCount,
                Goal = waterTarget,
                ProgressPercent = CalculateProgressPercent(log.GlassesCount, waterTarget)
            }).ToList();

        private static List<ClinicPatientProgressItemViewModel> BuildProgressEntries(
            IEnumerable<BodyProgress> progressEntries) =>
            progressEntries.Select(entry => new ClinicPatientProgressItemViewModel
            {
                Date = entry.Date,
                Weight = entry.Weight,
                Waist = entry.Waist,
                Hips = entry.Hips,
                Chest = entry.Chest,
                Arms = entry.Arms,
                Notes = entry.Notes,
                PhotoPath = entry.PhotoPath,
                CreatedAt = entry.CreatedAt
            }).ToList();

        private static ClinicPatientMealPlanSummaryViewModel BuildMealPlanSummary(MealPlan? mealPlan) =>
            mealPlan == null
                ? new ClinicPatientMealPlanSummaryViewModel()
                : new ClinicPatientMealPlanSummaryViewModel
                {
                    PlanId = mealPlan.Id,
                    HasPlan = true,
                    Title = mealPlan.Title,
                    PlanType = mealPlan.PlanType,
                    DietType = mealPlan.DietType,
                    CalorieTarget = mealPlan.CalorieTarget > 0 ? mealPlan.CalorieTarget : null,
                    CreatedAt = mealPlan.CreatedAt
                };

        private static void ApplyWeightSummary(
            ClinicPatientDetailsViewModel model,
            IReadOnlyList<WeightLog> weightLogs,
            IReadOnlyList<BodyProgress> progressEntries)
        {
            var latestWeight = weightLogs.FirstOrDefault()?.Weight;
            var latestProgressWeight = progressEntries
                .Where(p => p.Weight.HasValue)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefault()
                ?.Weight;

            model.CurrentWeight = latestWeight ?? latestProgressWeight ?? model.ProfileWeight;

            if (model.Height > 0 && model.CurrentWeight.HasValue)
            {
                var meters = model.Height.Value / 100.0;
                model.Bmi = Math.Round(model.CurrentWeight.Value / (meters * meters), 1);
            }

            if (weightLogs.Count >= 2)
            {
                var oldest = weightLogs[^1];
                var latest = weightLogs[0];
                model.WeightChange = Math.Round(latest.Weight - oldest.Weight, 1);
            }

            model.WeightTrendKey = model.WeightChange switch
            {
                < 0 => "clinic.patientDashboard.trend.decreasing",
                > 0 => "clinic.patientDashboard.trend.increasing",
                0 => "clinic.patientDashboard.trend.stable",
                _ => "clinic.patientDashboard.trend.insufficient"
            };
        }

        private static void ApplyNutritionSummary(
            ClinicPatientDetailsViewModel model,
            IReadOnlyList<DayLog> mealLogs,
            IReadOnlyList<FoodScan> foodScans)
        {
            model.RecentMealLogCount = mealLogs.Count;
            model.RecentFoodScanCount = foodScans.Count;

            if (mealLogs.Count == 0)
            {
                return;
            }

            model.RecentAvgCalories = Math.Round(mealLogs.Average(l => l.CaloriesConsumed), 0);
            model.RecentAvgProtein = Math.Round(mealLogs.Average(l => l.Protein), 1);
            model.RecentAvgCarbs = Math.Round(mealLogs.Average(l => l.Carbs), 1);
            model.RecentAvgFats = Math.Round(mealLogs.Average(l => l.Fats), 1);
        }

        private static void ApplyHydrationSummary(
            ClinicPatientDetailsViewModel model,
            IReadOnlyList<WaterLog> waterLogs)
        {
            var todayWater = waterLogs.FirstOrDefault(w => w.Date.Date == DateTime.Today)?.GlassesCount ?? 0;
            model.TodayWaterGlasses = todayWater;
            model.TodayWaterProgressPercent = CalculateProgressPercent(todayWater, model.WaterTarget);
        }

        private static void ApplyRecentActivity(
            ClinicPatientDetailsViewModel model,
            IReadOnlyList<WeightLog> weightLogs,
            IReadOnlyList<DayLog> mealLogs,
            IReadOnlyList<FoodScan> foodScans,
            IReadOnlyList<WaterLog> waterLogs,
            IReadOnlyList<BodyProgress> progressEntries,
            MealPlan? mealPlan)
        {
            var activities = new List<(DateTime At, string Key)>();

            if (weightLogs.Count > 0)
            {
                activities.Add((weightLogs[0].Date, "clinic.patientDashboard.activity.weight"));
            }

            if (mealLogs.Count > 0)
            {
                activities.Add((mealLogs[0].Date, "clinic.patientDashboard.activity.nutrition"));
            }

            if (foodScans.Count > 0)
            {
                activities.Add((foodScans[0].ScannedAt, "clinic.patientDashboard.activity.scan"));
            }

            if (waterLogs.Count > 0)
            {
                activities.Add((waterLogs[0].Date, "clinic.patientDashboard.activity.hydration"));
            }

            if (progressEntries.Count > 0)
            {
                activities.Add((progressEntries[0].CreatedAt, "clinic.patientDashboard.activity.progress"));
            }

            if (mealPlan != null)
            {
                activities.Add((mealPlan.CreatedAt, "clinic.patientDashboard.activity.mealPlan"));
            }

            if (activities.Count == 0)
            {
                return;
            }

            var latest = activities.OrderByDescending(a => a.At).First();
            model.RecentActivityAt = latest.At;
            model.RecentActivityKey = latest.Key;
        }

        private static int CalculateProgressPercent(int value, int target)
        {
            if (target <= 0)
            {
                return 0;
            }

            return Math.Clamp((int)Math.Round(value * 100.0 / target), 0, 100);
        }

        private static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        private static string NormalizeStatusFilter(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            PatientStatuses.Contains(status, StringComparer.Ordinal)
                ? status
                : "all";

        private static string NormalizeConsentFilter(string? consent) =>
            !string.IsNullOrWhiteSpace(consent) &&
            ConsentFilters.Contains(consent, StringComparer.Ordinal)
                ? consent
                : "all";

        private static string GenerateInvitationToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
