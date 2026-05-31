using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;
using ParkingReservation.Models.ViewModels;

namespace ParkingReservation.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard(ParkingLotStatus? status = null)
        {
            var parkingLotsQuery = _context.ParkingLots
                .Include(parkingLot => parkingLot.Owner)
                .Include(parkingLot => parkingLot.ParkingSpots)
                .AsQueryable();

            var viewModel = new AdminDashboardViewModel
            {
                TotalParkingLotsCount = await parkingLotsQuery.CountAsync(),
                PendingParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Pending),
                ApprovedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Approved),
                RejectedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Rejected),
                SuspendedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Suspended),
                SelectedStatus = status
            };

            if (status.HasValue)
            {
                parkingLotsQuery = parkingLotsQuery.Where(parkingLot => parkingLot.Status == status.Value);
            }

            viewModel.ParkingLots = await parkingLotsQuery
                .OrderBy(parkingLot => parkingLot.Status)
                .ThenByDescending(parkingLot => parkingLot.CreatedAt)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> ParkingLotDetails(int id)
        {
            var parkingLot = await _context.ParkingLots
                .Include(item => item.Owner)
                .Include(item => item.ParkingSpots.OrderBy(spot => spot.SpotNumber))
                .Include(item => item.Reviews)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (parkingLot == null)
            {
                return NotFound();
            }

            return View(parkingLot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeParkingLotStatus(int id, ParkingLotStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                return BadRequest();
            }

            var parkingLot = await _context.ParkingLots.FindAsync(id);

            if (parkingLot == null)
            {
                return NotFound();
            }

            parkingLot.Status = status;
            parkingLot.IsActive = status == ParkingLotStatus.Approved;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Parking lot status changed to {status}.";
            return RedirectToAction(nameof(Dashboard), new { status });
        }
    }
}
