namespace MoodBite.ViewModels.Clinic
{
    public class ClinicDashboardShellViewModel
    {
        public bool HasActiveClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public bool IsPlatformAdmin { get; set; }
        public int? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public int ActivePatients { get; set; }
        public int PatientsNeedingFollowUp { get; set; }
        public int ReportsReady { get; set; }
        public int UpcomingAppointments { get; set; }
        public List<ClinicAppointmentListItemViewModel> NextAppointments { get; set; } = new();
    }
}
