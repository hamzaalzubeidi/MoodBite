namespace MoodBite.ViewModels.Clinic
{
    public class ClinicPatientsIndexViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? Query { get; set; }
        public string StatusFilter { get; set; } = "all";
        public string ConsentFilter { get; set; } = "all";
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int PendingPatients { get; set; }
        public int PendingInvitations { get; set; }
        public string? LastInvitationLink { get; set; }
        public List<ClinicPatientRosterItemViewModel> Patients { get; set; } = new();
        public List<ClinicPatientInvitationItemViewModel> Invitations { get; set; } = new();
    }

    public class ClinicPatientRosterItemViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ConsentGranted { get; set; }
        public DateTime LinkedAt { get; set; }
        public string? PrimaryDietitianName { get; set; }
        public string? DietSlug { get; set; }
        public string? Goal { get; set; }
        public double? Weight { get; set; }
        public double? CalorieTarget { get; set; }
        public DateTime? ProfileUpdatedAt { get; set; }
    }

    public class ClinicPatientInvitationItemViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? InvitedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }

    public class ClinicInvitePatientViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class ClinicLinkExistingPatientViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class ClinicAcceptInvitationViewModel
    {
        public string Token { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public bool IsValid { get; set; }
        public bool IsAccepted { get; set; }
        public string? ClinicName { get; set; }
        public string? Email { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Message { get; set; }
    }
}
