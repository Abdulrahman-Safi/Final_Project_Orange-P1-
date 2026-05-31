using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models
{
    public class ParkingSpot
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SpotNumber { get; set; } = string.Empty;

        public ParkingSpotType SpotType { get; set; } = ParkingSpotType.Standard;

        public ParkingSpotStatus Status { get; set; } = ParkingSpotStatus.Available;

        public bool IsActive { get; set; } = true;

        public int ParkingLotId { get; set; }

        public ParkingLot? ParkingLot { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
