namespace MoodBite.ViewModels.Clinic
{
    public class ClinicSettingsViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public bool CanEditActiveStatus { get; set; }
        public int ClinicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}
