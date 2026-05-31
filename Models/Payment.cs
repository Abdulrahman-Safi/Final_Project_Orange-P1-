using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingReservation.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(150)]
        public string? TransactionId { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Now;

        public int ReservationId { get; set; }

        public Reservation? Reservation { get; set; }
    }
}
