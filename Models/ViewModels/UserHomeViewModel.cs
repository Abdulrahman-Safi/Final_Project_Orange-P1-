namespace ParkingReservation.Models.ViewModels
{
    public class UserHomeViewModel
    {
        public List<ParkingLot> NearbyParkingLots { get; set; } = new();

        public List<Reservation> ActiveReservations { get; set; } = new();

        public List<Reservation> ReservationHistory { get; set; } = new();

        public List<ParkingLot> FavoriteParkingLots { get; set; } = new();

        public int AvailableSpotsCount { get; set; }
    }
}
