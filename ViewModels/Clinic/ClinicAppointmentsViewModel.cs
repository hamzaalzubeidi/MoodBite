namespace MoodBite.ViewModels.Clinic
{
    public class ClinicAppointmentsIndexViewModel
    {
        public bool HasClinicContext { get; set; }
        public bool ClinicDataUnavailable { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string Filter { get; set; } = "upcoming";
        public string? PatientId { get; set; }
        public List<ClinicAppointmentListItemViewModel> Appointments { get; set; } = new();
        public List<ClinicAppointmentPatientOptionViewModel> Patients { get; set; } = new();
        public int UpcomingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class ClinicAppointmentEditorViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public int? AppointmentId { get; set; }
        public bool IsNew { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public List<ClinicAppointmentPatientOptionViewModel> Patients { get; set; } = new();
        public DateTime StartsAt { get; set; } = DateTime.Today.AddDays(1).AddHours(9);
        public int DurationMinutes { get; set; } = 30;
        public string Status { get; set; } = "scheduled";
        public string VisitType { get; set; } = "followUp";
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class ClinicAppointmentDetailsViewModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public ClinicAppointmentListItemViewModel Appointment { get; set; } = new();
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    public class ClinicAppointmentInputViewModel
    {
        public int ClinicId { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public string Status { get; set; } = "scheduled";
        public string VisitType { get; set; } = "followUp";
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class ClinicAppointmentListItemViewModel
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientEmail { get; set; }
        public string DietitianName { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "scheduled";
        public string VisitType { get; set; } = "followUp";
        public string? Location { get; set; }
    }

    public class ClinicAppointmentPatientOptionViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
