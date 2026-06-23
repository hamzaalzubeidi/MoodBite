namespace MoodBite.ViewModels
{
    public class AuditLogIndexViewModel
    {
        public int? ClinicId { get; set; }
        public string? Action { get; set; }
        public string? Query { get; set; }
        public int Take { get; set; } = 100;
        public bool IsClinicScoped { get; set; }
        public List<AuditLogListItemViewModel> Logs { get; set; } = new();
    }

    public class AuditLogListItemViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? ActorEmail { get; set; }
        public string? ActorRoles { get; set; }
        public int? ClinicId { get; set; }
        public string TargetEntityType { get; set; } = string.Empty;
        public string? TargetEntityId { get; set; }
        public string? TargetUserId { get; set; }
        public string? IpAddress { get; set; }
    }
}
