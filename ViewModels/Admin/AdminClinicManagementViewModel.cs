namespace MoodBite.ViewModels.Admin
{
    public class AdminClinicManagementViewModel
    {
        public bool ClinicDataUnavailable { get; set; }
        public List<AdminClinicListItem> Clinics { get; set; } = new();
    }

    public class AdminClinicListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
        public int PatientCount { get; set; }
    }

    public class AdminCreateClinicViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
    }

    public class AdminAssignClinicOwnerViewModel
    {
        public int ClinicId { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
