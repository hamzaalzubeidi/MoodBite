namespace MoodBite.ViewModels.Clinic
{
    public class ClinicStaffIndexViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public List<ClinicStaffMemberViewModel> Members { get; set; } = new();
    }

    public class ClinicStaffMemberViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class ClinicAddStaffMemberViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
