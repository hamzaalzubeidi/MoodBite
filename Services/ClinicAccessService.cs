using Microsoft.EntityFrameworkCore;
using MoodBite.Constants;
using MoodBite.Data;

namespace MoodBite.Services
{
    public class ClinicAccessService
    {
        private static readonly string[] ActivePatientStatuses = ["active"];

        private readonly ApplicationDbContext _db;
        private readonly CurrentUserService _currentUser;

        public ClinicAccessService(ApplicationDbContext db, CurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public Task<bool> IsPlatformAdminAsync(string? userId, CancellationToken cancellationToken = default) =>
            _currentUser.IsUserPlatformAdminAsync(userId, cancellationToken);

        public async Task<bool> IsClinicMemberAsync(
            string? userId,
            int clinicId,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || clinicId <= 0)
            {
                return false;
            }

            var query = _db.ClinicMembers.AsNoTracking()
                .Where(member => member.UserId == userId && member.ClinicId == clinicId);

            if (activeOnly)
            {
                query = query.Where(member => member.IsActive && member.Clinic.IsActive);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<bool> HasClinicRoleAsync(
            string? userId,
            int clinicId,
            string role,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(role) ||
                clinicId <= 0 ||
                !ApplicationRoles.FutureClinicRoles.Contains(role, StringComparer.Ordinal))
            {
                return false;
            }

            var query = _db.ClinicMembers.AsNoTracking()
                .Where(member =>
                    member.UserId == userId &&
                    member.ClinicId == clinicId &&
                    member.Role == role);

            if (activeOnly)
            {
                query = query.Where(member => member.IsActive && member.Clinic.IsActive);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public Task<bool> IsClinicOwnerAsync(
            string? userId,
            int clinicId,
            CancellationToken cancellationToken = default) =>
            HasClinicRoleAsync(userId, clinicId, ApplicationRoles.ClinicOwner, cancellationToken: cancellationToken);

        public Task<bool> IsDietitianAsync(
            string? userId,
            int clinicId,
            CancellationToken cancellationToken = default) =>
            HasClinicRoleAsync(userId, clinicId, ApplicationRoles.Dietitian, cancellationToken: cancellationToken);

        public Task<bool> IsClinicStaffAsync(
            string? userId,
            int clinicId,
            CancellationToken cancellationToken = default) =>
            HasClinicRoleAsync(userId, clinicId, ApplicationRoles.ClinicStaff, cancellationToken: cancellationToken);

        public async Task<bool> CanManageClinicAsync(
            string? userId,
            int clinicId,
            CancellationToken cancellationToken = default)
        {
            if (clinicId <= 0)
            {
                return false;
            }

            return await IsPlatformAdminAsync(userId, cancellationToken) ||
                   await IsClinicOwnerAsync(userId, clinicId, cancellationToken);
        }

        public async Task<bool> CanAccessPatientAsync(
            string? clinicianUserId,
            int clinicId,
            string? patientId,
            bool requireConsent = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clinicianUserId) ||
                string.IsNullOrWhiteSpace(patientId) ||
                clinicId <= 0)
            {
                return false;
            }

            var isMember = await IsClinicMemberAsync(clinicianUserId, clinicId, cancellationToken: cancellationToken);
            if (!isMember)
            {
                return false;
            }

            return await PatientBelongsToClinicAsync(
                patientId,
                clinicId,
                activeOnly: true,
                requireConsent: requireConsent,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> PatientBelongsToClinicAsync(
            string? patientId,
            int clinicId,
            bool activeOnly = true,
            bool requireConsent = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(patientId) || clinicId <= 0)
            {
                return false;
            }

            var query = _db.ClinicPatients.AsNoTracking()
                .Where(patient => patient.PatientId == patientId && patient.ClinicId == clinicId);

            if (activeOnly)
            {
                query = query.Where(patient =>
                    ActivePatientStatuses.Contains(patient.Status) &&
                    patient.Clinic.IsActive);
            }

            if (requireConsent)
            {
                query = query.Where(patient => patient.ConsentGranted);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<int?> ResolveActiveClinicIdAsync(
            string? userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var clinicIds = await _db.ClinicMembers.AsNoTracking()
                .Where(member => member.UserId == userId && member.IsActive && member.Clinic.IsActive)
                .OrderBy(member => member.JoinedAt)
                .Select(member => member.ClinicId)
                .Distinct()
                .Take(2)
                .ToListAsync(cancellationToken);

            return clinicIds.Count == 1 ? clinicIds[0] : null;
        }

        public Task<int?> ResolveCurrentUserActiveClinicIdAsync(CancellationToken cancellationToken = default) =>
            ResolveActiveClinicIdAsync(_currentUser.GetCurrentUserId(), cancellationToken);
    }
}
