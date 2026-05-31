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

        public async Task<IActionResult> Index()
        {
            var parkingLots = await _context.ParkingLots
                .Include(parkingLot => parkingLot.ParkingSpots)
                .Where(parkingLot =>
                    parkingLot.IsActive &&
                    parkingLot.Status == ParkingLotStatus.Approved)
                .OrderBy(parkingLot => parkingLot.City)
                .ThenBy(parkingLot => parkingLot.Name)
                .ToListAsync();

            return View(parkingLots);
        }

        public async Task<IActionResult> Details(int id)
        {
            var parkingLot = await _context.ParkingLots
                .Include(parkingLot => parkingLot.Owner)
                .Include(parkingLot => parkingLot.ParkingSpots.Where(spot => spot.IsActive))
                .FirstOrDefaultAsync(parkingLot =>
                    parkingLot.Id == id &&
                    parkingLot.IsActive &&
                    parkingLot.Status == ParkingLotStatus.Approved);

            if (parkingLot == null)
            {
                return NotFound();
            }

            return View(parkingLot);
        }
    }
}
