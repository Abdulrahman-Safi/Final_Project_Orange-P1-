using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models.ViewModels
{
    public class ParkingSpotFormViewModel
    {
        public int ParkingLotId { get; set; }

        public string ParkingLotName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الموقف")]
        public string SpotNumber { get; set; } = string.Empty;

        [Display(Name = "نوع الموقف")]
        public ParkingSpotType SpotType { get; set; } = ParkingSpotType.Standard;
    }
}
