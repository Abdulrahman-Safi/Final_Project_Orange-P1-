namespace ParkingReservation.Models
{
    public enum ParkingLotStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Suspended = 4
    }

    public enum ParkingSpotStatus
    {
        Available = 1,
        Reserved = 2,
        Occupied = 3,
        Maintenance = 4
    }

    public enum ParkingSpotType
    {
        Standard = 1,
        Disabled = 2,
        Electric = 3,
        VIP = 4
    }

    public enum ReservationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3,
        Completed = 4
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4
    }

    public enum PaymentMethod
    {
        CreditCard = 1,
        PayPal = 2,
        Wallet = 3
    }
}
