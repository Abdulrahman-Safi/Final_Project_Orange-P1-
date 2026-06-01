using System.ComponentModel.DataAnnotations;

namespace ParkingReservation.Models.ViewModels
{
    public class ParkingLotFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المصف مطلوب.")]
        [StringLength(150)]
        [Display(Name = "اسم المصف")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة.")]
        [StringLength(100)]
        [Display(Name = "المدينة")]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "المنطقة / الحي")]
        public string? Area { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب.")]
        [StringLength(250)]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب تحديد موقع المصف على الخريطة.")]
        [Range(-90, 90, ErrorMessage = "خط العرض غير صحيح.")]
        [Display(Name = "خط العرض")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "يجب تحديد موقع المصف على الخريطة.")]
        [Range(-180, 180, ErrorMessage = "خط الطول غير صحيح.")]
        [Display(Name = "خط الطول")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "وقت الفتح مطلوب.")]
        [Display(Name = "وقت الفتح")]
        public TimeSpan OpeningTime { get; set; } = new(8, 0, 0);

        [Required(ErrorMessage = "وقت الإغلاق مطلوب.")]
        [Display(Name = "وقت الإغلاق")]
        public TimeSpan ClosingTime { get; set; } = new(22, 0, 0);

        [Range(0.01, 999.99, ErrorMessage = "سعر الساعة يجب أن يكون بين 0.01 و 999.99.")]
        [Display(Name = "سعر الساعة")]
        public decimal HourlyRate { get; set; }
    }
}
