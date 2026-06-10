using System.ComponentModel.DataAnnotations;

namespace MoodBite.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // healthy, mediterranean, vegan, etc.
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public double Rating { get; set; }
    }
}
