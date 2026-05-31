using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models.ViewModels
{
    public class ParkingLotFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        [Required]
        [Display(Name = "Opening time")]
        public TimeSpan OpeningTime { get; set; } = new(8, 0, 0);

        [Required]
        [Display(Name = "Closing time")]
        public TimeSpan ClosingTime { get; set; } = new(22, 0, 0);

        [Range(0.01, 999.99)]
        [Display(Name = "Hourly rate")]
        public decimal HourlyRate { get; set; }
    }
}
