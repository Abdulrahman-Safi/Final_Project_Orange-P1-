using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int ParkingLotId { get; set; }

        public ParkingLot? ParkingLot { get; set; }
    }
}
