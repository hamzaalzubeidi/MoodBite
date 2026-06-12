using Microsoft.EntityFrameworkCore;
using MoodBite.Data;
using MoodBite.ViewModels.Clinic;

namespace MoodBite.Services
{
    public sealed record ClinicPatientAccessContext(int ClinicId, string ClinicName, string PatientId);

    public class ClinicPatientAccessContextService
    {
        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;

        public ClinicPatientAccessContextService(
            ApplicationDbContext db,
            CurrentUserService currentUser,
            ClinicAccessService clinicAccess)
        {
            _db = db;
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
        }

        public async Task<int?> ResolveAccessibleClinicIdAsync(int? clinicId, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            if (clinicId.HasValue)
            {
                return await CanAccessClinicAsync(clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolved = await _clinicAccess.ResolveActiveClinicIdAsync(currentUserId, cancellationToken);
            if (!resolved.HasValue)
            {
                return null;
            }

            return await CanAccessClinicAsync(resolved.Value, cancellationToken)
                ? resolved.Value
                : null;
        }

        public async Task<ClinicPatientAccessContext?> ResolvePatientAccessAsync(
            string? patientId,
            int? clinicId,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(patientId))
            {
                return null;
            }

            var resolvedClinicId = await ResolveClinicForPatientAsync(
                currentUserId,
                patientId,
                clinicId,
                cancellationToken);
            if (!resolvedClinicId.HasValue)
            {
                return null;
            }

            var isPlatformAdmin = await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken);
            var isClinicMember = await _clinicAccess.IsClinicMemberAsync(
                currentUserId,
                resolvedClinicId.Value,
                cancellationToken: cancellationToken);
            if (!isPlatformAdmin && !isClinicMember)
            {
                return null;
            }

            var patientBelongsToClinic = await _clinicAccess.PatientBelongsToClinicAsync(
                patientId,
                resolvedClinicId.Value,
                activeOnly: false,
                requireConsent: false,
                cancellationToken: cancellationToken);
            if (!patientBelongsToClinic)
            {
                return null;
            }

            var clinic = await _db.Clinics.AsNoTracking()
                .Where(c => c.Id == resolvedClinicId.Value && c.IsActive)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return clinic == null
                ? null
                : new ClinicPatientAccessContext(clinic.Id, clinic.Name, patientId);
        }

        public async Task<bool> CanAccessClinicAsync(int clinicId, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId) || clinicId <= 0)
            {
                return false;
            }

            return await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken) ||
                   await _clinicAccess.IsClinicMemberAsync(currentUserId, clinicId, cancellationToken: cancellationToken);
        }

        public async Task<ClinicPatientCompactViewModel?> BuildPatientSummaryAsync(
            string patientId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Users.AsNoTracking()
                .Where(u => u.Id == patientId)
                .Select(u => new ClinicPatientCompactViewModel
                {
                    PatientId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Goal = u.HealthProfile != null ? u.HealthProfile.Goal : null,
                    DietSlug = u.HealthProfile != null ? u.HealthProfile.DietSlug : null
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<int?> ResolveClinicForPatientAsync(
            string currentUserId,
            string patientId,
            int? clinicId,
            CancellationToken cancellationToken)
        {
            if (clinicId.HasValue)
            {
                return await CanAccessClinicAsync(clinicId.Value, cancellationToken)
                    ? clinicId.Value
                    : null;
            }

            var resolvedClinicId = await ResolveAccessibleClinicIdAsync(null, cancellationToken);
            if (resolvedClinicId.HasValue)
            {
                return resolvedClinicId.Value;
            }

            if (!await _clinicAccess.IsPlatformAdminAsync(currentUserId, cancellationToken))
            {
                return null;
            }

            var clinicIds = await _db.ClinicPatients.AsNoTracking()
                .Where(p => p.PatientId == patientId && p.Clinic.IsActive)
                .OrderByDescending(p => p.LinkedAt)
                .Select(p => p.ClinicId)
                .Distinct()
                .Take(2)
                .ToListAsync(cancellationToken);

            return clinicIds.Count == 1 ? clinicIds[0] : null;
        }
    }
}
