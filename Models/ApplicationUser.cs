using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public ICollection<ParkingLot> OwnedParkingLots { get; set; } = new List<ParkingLot>();

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
