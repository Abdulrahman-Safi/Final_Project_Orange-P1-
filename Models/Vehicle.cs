using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Make { get; set; }

        [StringLength(50)]
        public string? Model { get; set; }

        [StringLength(30)]
        public string? Color { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
