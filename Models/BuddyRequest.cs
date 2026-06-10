using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class BuddyRequest
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser Sender { get; set; } = null!;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        public ApplicationUser Receiver { get; set; } = null!;

        public string Status { get; set; } = "pending"; // pending / accepted / declined
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Message { get; set; }
    }
}
