using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models.ViewModels
{
    public class ParkingSpotFormViewModel
    {
        public int ParkingLotId { get; set; }

        public string ParkingLotName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Spot number")]
        public string SpotNumber { get; set; } = string.Empty;

        [Display(Name = "Spot type")]
        public ParkingSpotType SpotType { get; set; } = ParkingSpotType.Standard;
    }
}
