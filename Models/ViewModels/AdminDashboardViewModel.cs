namespace ParkingReservation.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalParkingLotsCount { get; set; }

        public int PendingParkingLotsCount { get; set; }

        public int ApprovedParkingLotsCount { get; set; }

        public int RejectedParkingLotsCount { get; set; }

        public int SuspendedParkingLotsCount { get; set; }

        public int TotalUsersCount { get; set; }

        public int TotalOwnersCount { get; set; }

        public int TotalReservationsCount { get; set; }

        public ParkingLotStatus? SelectedStatus { get; set; }

        public List<ParkingLot> ParkingLots { get; set; } = new();

        public List<ApplicationUser> RecentUsers { get; set; } = new();

        public List<ParkingLot> RecentlyAddedParkingLots { get; set; } = new();

        public List<string> ActivityLogs { get; set; } = new();

        public List<DashboardChartItem> NewUsersGrowth { get; set; } = new();

        public List<DashboardChartItem> ReservationsStatistics { get; set; } = new();

        public List<DashboardChartItem> RevenueAnalytics { get; set; } = new();
    }
}
