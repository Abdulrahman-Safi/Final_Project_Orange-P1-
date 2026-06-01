using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Controllers
{
    public class ParkingLotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParkingLotsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startTime, DateTime? endTime, string? city, string? area)
        {
            var hasValidPeriod = startTime.HasValue && endTime.HasValue && endTime > startTime;
            var now = DateTime.Now;

            // Fetch all approved lots
            var allLots = await _context.ParkingLots
                .Include(l => l.Owner)
                .Include(l => l.ParkingSpots.Where(spot =>
                    spot.IsActive &&
                    spot.Status == ParkingSpotStatus.Available &&
                    (hasValidPeriod
                        ? !spot.Reservations.Any(r =>
                            r.Status != ReservationStatus.Cancelled &&
                            startTime!.Value < r.EndTime &&
                            endTime!.Value > r.StartTime)
                        : !spot.Reservations.Any(r =>
                            r.Status == ReservationStatus.Confirmed &&
                            r.StartTime <= now &&
                            r.EndTime > now))))
                .Where(l =>
                    l.IsActive &&
                    l.Status == ParkingLotStatus.Approved &&
                    l.Owner!.IsActive)
                .OrderBy(l => l.Area ?? l.City)
                .ThenBy(l => l.Name)
                .ToListAsync();

            // Distinct city list
            var cities = allLots
                .Select(l => l.City)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Areas that actually have lots (for showing active badge count)
            var areasWithLots = allLots
                .Where(l => !string.IsNullOrEmpty(l.Area))
                .Select(l => l.Area!)
                .Distinct()
                .ToHashSet();

            var parkingLots = allLots.AsEnumerable();

            // Apply city filter
            if (!string.IsNullOrWhiteSpace(city))
                parkingLots = parkingLots.Where(l => l.City == city);

            // Apply area filter
            if (!string.IsNullOrWhiteSpace(area))
                parkingLots = parkingLots.Where(l => l.Area == area);

            // Apply time filter
            var filteredLots = parkingLots.ToList();
            if (hasValidPeriod)
                filteredLots = filteredLots.Where(l => l.ParkingSpots.Any()).ToList();

            ViewBag.StartTime = startTime?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.EndTime = endTime?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.HasValidPeriod = hasValidPeriod;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedArea = area;
            ViewBag.Cities = cities;
            ViewBag.AllAreas = AmmanAreas.All;
            ViewBag.AreasWithLots = areasWithLots;

            return View(filteredLots);
        }

        public async Task<IActionResult> Details(int id, DateTime? startTime, DateTime? endTime)
        {
            var hasValidPeriod = startTime.HasValue && endTime.HasValue && endTime > startTime;
            var now = DateTime.Now;

            var parkingLot = await _context.ParkingLots
                .Include(l => l.Owner)
                .Include(l => l.ParkingSpots.Where(spot =>
                    spot.IsActive &&
                    spot.Status == ParkingSpotStatus.Available &&
                    (hasValidPeriod
                        ? !spot.Reservations.Any(r =>
                            r.Status != ReservationStatus.Cancelled &&
                            startTime!.Value < r.EndTime &&
                            endTime!.Value > r.StartTime)
                        : !spot.Reservations.Any(r =>
                            r.Status == ReservationStatus.Confirmed &&
                            r.StartTime <= now &&
                            r.EndTime > now))))
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(l =>
                    l.Id == id &&
                    l.IsActive &&
                    l.Status == ParkingLotStatus.Approved &&
                    l.Owner!.IsActive);

            if (parkingLot == null)
                return NotFound();

            ViewBag.StartTime = startTime?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.EndTime = endTime?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.HasValidPeriod = hasValidPeriod;

            return View(parkingLot);
        }
    }
}
