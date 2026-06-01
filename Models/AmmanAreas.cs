namespace ParkingReservation.Models
{
    /// <summary>
    /// Predefined list of common Amman neighborhoods / areas.
    /// Used as the authoritative source for the Area dropdown throughout the application.
    /// </summary>
    public static class AmmanAreas
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "طبربور",
            "الجبيهة",
            "الجاردنز",
            "شميساني",
            "العبدلي",
            "وسط البلد",
            "الصويفية",
            "خلدا",
            "دابوق",
            "تلاع العلي",
            "شارع الجامعة",
            "ماركا",
            "سحاب",
            "الويبدة",
            "جبل عمان",
            "جبل الحسين"
        };
    }
}
