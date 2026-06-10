namespace MoodBite.Services
{
    public enum PatientAccessMode
    {
        Self = 1,
        Clinic = 2
    }

    public sealed record PatientAccessContext(
        string PatientId,
        int? ClinicId,
        PatientAccessMode AccessMode);

    public class PatientContextService
    {
        private readonly CurrentUserService _currentUser;
        private readonly ClinicAccessService _clinicAccess;

        public PatientContextService(CurrentUserService currentUser, ClinicAccessService clinicAccess)
        {
            _currentUser = currentUser;
            _clinicAccess = clinicAccess;
        }

        public string? GetCurrentPatientId() => _currentUser.GetCurrentUserId();

        public bool IsCurrentUserPatient(string? patientId)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            return !string.IsNullOrWhiteSpace(currentUserId) &&
                   !string.IsNullOrWhiteSpace(patientId) &&
                   string.Equals(currentUserId, patientId, StringComparison.Ordinal);
        }

        public async Task<bool> CanCurrentUserAccessPatientAsync(
            string? patientId,
            int? clinicId = null,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(patientId))
            {
                return false;
            }

            if (string.Equals(currentUserId, patientId, StringComparison.Ordinal))
            {
                return true;
            }

            return clinicId.HasValue &&
                   await _clinicAccess.CanAccessPatientAsync(
                       currentUserId,
                       clinicId.Value,
                       patientId,
                       cancellationToken: cancellationToken);
        }

        public async Task<PatientAccessContext?> ResolvePatientAccessAsync(
            string? requestedPatientId = null,
            int? clinicId = null,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            var patientId = string.IsNullOrWhiteSpace(requestedPatientId)
                ? currentUserId
                : requestedPatientId;

            if (string.Equals(currentUserId, patientId, StringComparison.Ordinal))
            {
                if (!clinicId.HasValue ||
                    await _clinicAccess.PatientBelongsToClinicAsync(
                        patientId,
                        clinicId.Value,
                        cancellationToken: cancellationToken))
                {
                    return new PatientAccessContext(patientId, clinicId, PatientAccessMode.Self);
                }

                return null;
            }

            if (clinicId.HasValue &&
                await _clinicAccess.CanAccessPatientAsync(
                    currentUserId,
                    clinicId.Value,
                    patientId,
                    cancellationToken: cancellationToken))
            {
                return new PatientAccessContext(patientId, clinicId, PatientAccessMode.Clinic);
            }

            return null;
        }

        public Task<bool> PatientBelongsToClinicAsync(
            string? patientId,
            int clinicId,
            bool activeOnly = true,
            bool requireConsent = false,
            CancellationToken cancellationToken = default) =>
            _clinicAccess.PatientBelongsToClinicAsync(
                patientId,
                clinicId,
                activeOnly,
                requireConsent,
                cancellationToken);

        public Task<int?> ResolveCurrentClinicianClinicIdAsync(CancellationToken cancellationToken = default) =>
            _clinicAccess.ResolveCurrentUserActiveClinicIdAsync(cancellationToken);
    }
}
