namespace ParkingReservation.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalParkingLotsCount { get; set; }

        public int PendingParkingLotsCount { get; set; }

        public int ApprovedParkingLotsCount { get; set; }

        public int RejectedParkingLotsCount { get; set; }

        public int SuspendedParkingLotsCount { get; set; }

        public ParkingLotStatus? SelectedStatus { get; set; }

        public List<ParkingLot> ParkingLots { get; set; } = new();
    }
}
