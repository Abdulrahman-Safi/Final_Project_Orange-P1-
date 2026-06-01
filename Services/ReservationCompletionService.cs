using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Services
{
    public class ReservationCompletionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCompletionService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public ReservationCompletionService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationCompletionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء مزامنة حالة الحجوزات والمواقف.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task SyncAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.Now;

            // 1. Mark confirmed reservations whose EndTime has passed as Completed
            var expired = await context.Reservations
                .Where(r => r.Status == ReservationStatus.Confirmed && r.EndTime <= now)
                .ToListAsync();

            foreach (var r in expired)
                r.Status = ReservationStatus.Completed;

            if (expired.Count > 0)
                _logger.LogInformation("تم إكمال {Count} حجز منتهي الوقت.", expired.Count);

            // 2. Free spots that have no remaining confirmed reservations (future or active)
            var spotsToFree = await context.ParkingSpots
                .Where(s => s.Status == ParkingSpotStatus.Reserved
                         && !s.Reservations.Any(r => r.Status == ReservationStatus.Confirmed))
                .ToListAsync();

            foreach (var spot in spotsToFree)
                spot.Status = ParkingSpotStatus.Available;

            await context.SaveChangesAsync();
        }
    }
}
