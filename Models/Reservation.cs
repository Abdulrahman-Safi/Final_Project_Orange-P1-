using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingReservation.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int ParkingSpotId { get; set; }

        public ParkingSpot? ParkingSpot { get; set; }

        public int? VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Payment? Payment { get; set; }
    }
}
