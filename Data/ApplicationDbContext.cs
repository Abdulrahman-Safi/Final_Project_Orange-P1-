using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Models;

namespace ParkingReservation.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ParkingLot> ParkingLots { get; set; }

        public DbSet<ParkingSpot> ParkingSpots { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ParkingLot>()
                .HasOne(parkingLot => parkingLot.Owner)
                .WithMany(owner => owner.OwnedParkingLots)
                .HasForeignKey(parkingLot => parkingLot.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ParkingSpot>()
                .HasIndex(parkingSpot => new { parkingSpot.ParkingLotId, parkingSpot.SpotNumber })
                .IsUnique();

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.User)
                .WithMany(user => user.Reservations)
                .HasForeignKey(reservation => reservation.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Vehicle)
                .WithMany(vehicle => vehicle.Reservations)
                .HasForeignKey(reservation => reservation.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Payment>()
                .HasOne(payment => payment.Reservation)
                .WithOne(reservation => reservation.Payment)
                .HasForeignKey<Payment>(payment => payment.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(review => review.User)
                .WithMany(user => user.Reviews)
                .HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
