namespace ParkingReservation.Models.ViewModels
{
    public class OwnerDashboardViewModel
    {
        public int ParkingLotsCount { get; set; }

        public int ParkingSpotsCount { get; set; }

        public int AvailableSpotsCount { get; set; }

        public int ActiveReservationsCount { get; set; }

        public List<ParkingLot> ParkingLots { get; set; } = new();
    }
}
