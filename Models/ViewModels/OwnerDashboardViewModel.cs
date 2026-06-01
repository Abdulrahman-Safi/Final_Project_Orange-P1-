namespace ParkingReservation.Models.ViewModels
{
    public class OwnerDashboardViewModel
    {
        public int ParkingLotsCount { get; set; }

        public int ParkingSpotsCount { get; set; }

        public int AvailableSpotsCount { get; set; }

        public int ActiveReservationsCount { get; set; }

        public decimal MonthlyRevenue { get; set; }

        public List<ParkingLot> ParkingLots { get; set; } = new();

        public List<Reservation> RecentReservations { get; set; } = new();

        public List<ParkingLot> MostBookedParkingLots { get; set; } = new();

        public List<DashboardChartItem> ReservationsPerDay { get; set; } = new();

        public List<DashboardChartItem> RevenuePerMonth { get; set; } = new();
    }
}
