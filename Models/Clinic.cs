using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class Clinic
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(160)]
        public string? LegalName { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(40)]
        public string? Phone { get; set; }

        [MaxLength(120)]
        public string? Country { get; set; }

        [MaxLength(120)]
        public string? City { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ClinicMember> Members { get; set; } = new List<ClinicMember>();
        public ICollection<ClinicPatient> Patients { get; set; } = new List<ClinicPatient>();
        public ICollection<ClinicInvitation> Invitations { get; set; } = new List<ClinicInvitation>();
        public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
