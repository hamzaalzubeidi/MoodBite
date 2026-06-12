using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.Models;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Services
{
    public class ClinicAppointmentsService
    {
        public static readonly string[] Statuses = ["scheduled", "completed", "cancelled"];
        public static readonly string[] Filters = ["upcoming", "completed", "cancelled", "all"];

        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;

        public ClinicAppointmentsService(ApplicationDbContext db, CurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<List<ClinicAppointmentPatientOptionViewModel>> GetPatientOptionsAsync(
            int clinicId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ClinicPatients.AsNoTracking()
                .Where(p => p.ClinicId == clinicId && p.Status == "active")
                .OrderBy(p => p.Patient.FullName)
                .Select(p => new ClinicAppointmentPatientOptionViewModel
                {
                    PatientId = p.PatientId,
                    FullName = p.Patient.FullName,
                    Email = p.Patient.Email
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ClinicAppointmentsIndexViewModel> BuildIndexModelAsync(
            int clinicId,
            string clinicName,
            string? filter,
            string? patientId,
            CancellationToken cancellationToken = default)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var now = DateTime.UtcNow;

            var query = _db.Appointments.AsNoTracking()
                .Where(a => a.ClinicId == clinicId);

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                query = query.Where(a => a.PatientId == patientId);
            }

            query = normalizedFilter switch
            {
                "completed" => query.Where(a => a.Status == "completed"),
                "cancelled" => query.Where(a => a.Status == "cancelled"),
                "all" => query,
                _ => query.Where(a => a.Status == "scheduled" && a.StartsAt >= now)
            };

            query = normalizedFilter == "upcoming"
                ? query.OrderBy(a => a.StartsAt)
                : query.OrderByDescending(a => a.StartsAt);

            var appointments = await query
                .Take(100)
                .Select(a => new ClinicAppointmentListItemViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    PatientEmail = a.Patient.Email,
                    DietitianName = a.Dietitian.FullName,
                    StartsAt = a.StartsAt,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    VisitType = a.VisitType,
                    Location = a.Location
                })
                .ToListAsync(cancellationToken);

            return new ClinicAppointmentsIndexViewModel
            {
                HasClinicContext = true,
                ClinicId = clinicId,
                ClinicName = clinicName,
                Filter = normalizedFilter,
                PatientId = patientId,
                Appointments = appointments,
                Patients = await GetPatientOptionsAsync(clinicId, cancellationToken),
                UpcomingCount = await _db.Appointments.AsNoTracking()
                    .CountAsync(a => a.ClinicId == clinicId && a.Status == "scheduled" && a.StartsAt >= now, cancellationToken),
                CompletedCount = await _db.Appointments.AsNoTracking()
                    .CountAsync(a => a.ClinicId == clinicId && a.Status == "completed", cancellationToken),
                CancelledCount = await _db.Appointments.AsNoTracking()
                    .CountAsync(a => a.ClinicId == clinicId && a.Status == "cancelled", cancellationToken)
            };
        }

        public async Task<List<ClinicAppointmentListItemViewModel>> GetUpcomingAppointmentsAsync(
            int clinicId,
            int take = 5,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _db.Appointments.AsNoTracking()
                .Where(a => a.ClinicId == clinicId && a.Status == "scheduled" && a.StartsAt >= now)
                .OrderBy(a => a.StartsAt)
                .Take(take)
                .Select(a => new ClinicAppointmentListItemViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    PatientEmail = a.Patient.Email,
                    DietitianName = a.Dietitian.FullName,
                    StartsAt = a.StartsAt,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    VisitType = a.VisitType,
                    Location = a.Location
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ClinicAppointmentListItemViewModel>> GetPatientUpcomingAppointmentsAsync(
            int clinicId,
            string patientId,
            int take = 5,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _db.Appointments.AsNoTracking()
                .Where(a => a.ClinicId == clinicId &&
                            a.PatientId == patientId &&
                            a.Status == "scheduled" &&
                            a.StartsAt >= now)
                .OrderBy(a => a.StartsAt)
                .Take(take)
                .Select(a => new ClinicAppointmentListItemViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    PatientEmail = a.Patient.Email,
                    DietitianName = a.Dietitian.FullName,
                    StartsAt = a.StartsAt,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    VisitType = a.VisitType,
                    Location = a.Location
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<Appointment?> GetAppointmentAsync(
            int appointmentId,
            int clinicId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Dietitian)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClinicId == clinicId, cancellationToken);
        }

        public async Task<Appointment?> CreateAppointmentAsync(
            int clinicId,
            ClinicAppointmentInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            var appointment = new Appointment
            {
                ClinicId = clinicId,
                PatientId = input.PatientId,
                DietitianId = currentUserId,
                StartsAt = input.StartsAt,
                DurationMinutes = NormalizeDuration(input.DurationMinutes),
                Status = NormalizeStatus(input.Status),
                VisitType = NormalizeVisitType(input.VisitType),
                Location = NormalizeMax(input.Location, 200),
                Notes = NormalizeMax(input.Notes, 1000),
                CreatedAt = DateTime.UtcNow,
                CancelledAt = NormalizeStatus(input.Status) == "cancelled" ? DateTime.UtcNow : null
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync(cancellationToken);
            return appointment;
        }

        public async Task<bool> UpdateAppointmentAsync(
            Appointment appointment,
            ClinicAppointmentInputViewModel input,
            CancellationToken cancellationToken = default)
        {
            var oldStatus = appointment.Status;
            var newStatus = NormalizeStatus(input.Status);

            appointment.PatientId = input.PatientId;
            appointment.StartsAt = input.StartsAt;
            appointment.DurationMinutes = NormalizeDuration(input.DurationMinutes);
            appointment.Status = newStatus;
            appointment.VisitType = NormalizeVisitType(input.VisitType);
            appointment.Location = NormalizeMax(input.Location, 200);
            appointment.Notes = NormalizeMax(input.Notes, 1000);
            appointment.CancelledAt = newStatus == "cancelled"
                ? appointment.CancelledAt ?? DateTime.UtcNow
                : null;

            if (oldStatus != "cancelled" && newStatus == "cancelled")
            {
                appointment.CancelledAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public ClinicAppointmentEditorViewModel BuildEditorModel(
            int clinicId,
            string clinicName,
            List<ClinicAppointmentPatientOptionViewModel> patients,
            Appointment? appointment = null,
            ClinicAppointmentInputViewModel? input = null)
        {
            return new ClinicAppointmentEditorViewModel
            {
                ClinicId = clinicId,
                ClinicName = clinicName,
                AppointmentId = appointment?.Id,
                IsNew = appointment == null,
                Patients = patients,
                PatientId = input?.PatientId ?? appointment?.PatientId ?? string.Empty,
                StartsAt = input?.StartsAt ?? appointment?.StartsAt ?? DateTime.Today.AddDays(1).AddHours(9),
                DurationMinutes = input?.DurationMinutes ?? appointment?.DurationMinutes ?? 30,
                Status = input?.Status ?? appointment?.Status ?? "scheduled",
                VisitType = input?.VisitType ?? appointment?.VisitType ?? "followUp",
                Location = input?.Location ?? appointment?.Location,
                Notes = input?.Notes ?? appointment?.Notes
            };
        }

        public ClinicAppointmentDetailsViewModel BuildDetailsModel(
            int clinicId,
            string clinicName,
            Appointment appointment)
        {
            return new ClinicAppointmentDetailsViewModel
            {
                ClinicId = clinicId,
                ClinicName = clinicName,
                Appointment = new ClinicAppointmentListItemViewModel
                {
                    Id = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = appointment.Patient.FullName,
                    PatientEmail = appointment.Patient.Email,
                    DietitianName = appointment.Dietitian.FullName,
                    StartsAt = appointment.StartsAt,
                    DurationMinutes = appointment.DurationMinutes,
                    Status = appointment.Status,
                    VisitType = appointment.VisitType,
                    Location = appointment.Location
                },
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                CancelledAt = appointment.CancelledAt
            };
        }

        public static string NormalizeFilter(string? filter) =>
            !string.IsNullOrWhiteSpace(filter) &&
            Filters.Contains(filter, StringComparer.Ordinal)
                ? filter
                : "upcoming";

        public static string NormalizeStatus(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            Statuses.Contains(status, StringComparer.Ordinal)
                ? status
                : "scheduled";

        private static int NormalizeDuration(int duration) =>
            Math.Clamp(duration, 5, 480);

        private static string NormalizeVisitType(string? visitType)
        {
            var normalized = string.IsNullOrWhiteSpace(visitType) ? "followUp" : visitType.Trim();
            return normalized.Length > 40 ? normalized[..40] : normalized;
        }

        private static string? NormalizeMax(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }
    }
}
