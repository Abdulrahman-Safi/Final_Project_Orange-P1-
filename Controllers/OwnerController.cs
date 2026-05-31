using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;
using ParkingReservation.Models.ViewModels;

namespace ParkingReservation.Controllers
{
    [Authorize(Roles = $"{UserRoles.Owner},{UserRoles.Admin}")]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingLots = await _context.ParkingLots
                .Include(parkingLot => parkingLot.ParkingSpots)
                .Where(parkingLot => parkingLot.OwnerId == ownerId)
                .OrderByDescending(parkingLot => parkingLot.CreatedAt)
                .ToListAsync();

            var parkingLotIds = parkingLots.Select(parkingLot => parkingLot.Id).ToList();
            var activeReservationsCount = await _context.Reservations
                .CountAsync(reservation =>
                    parkingLotIds.Contains(reservation.ParkingSpot!.ParkingLotId) &&
                    (reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed));

            var viewModel = new OwnerDashboardViewModel
            {
                ParkingLots = parkingLots,
                ParkingLotsCount = parkingLots.Count,
                ParkingSpotsCount = parkingLots.Sum(parkingLot => parkingLot.ParkingSpots.Count),
                AvailableSpotsCount = parkingLots.Sum(parkingLot => parkingLot.ParkingSpots.Count(spot => spot.Status == ParkingSpotStatus.Available)),
                ActiveReservationsCount = activeReservationsCount
            };

            return View(viewModel);
        }

        public IActionResult CreateParkingLot()
        {
            return View(new ParkingLotFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParkingLot(ParkingLotFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var ownerId = _userManager.GetUserId(User);

            var parkingLot = new ParkingLot
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                City = viewModel.City,
                Address = viewModel.Address,
                Latitude = viewModel.Latitude,
                Longitude = viewModel.Longitude,
                OpeningTime = viewModel.OpeningTime,
                ClosingTime = viewModel.ClosingTime,
                HourlyRate = viewModel.HourlyRate,
                OwnerId = ownerId!,
                Status = ParkingLotStatus.Pending,
                IsActive = true
            };

            _context.ParkingLots.Add(parkingLot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking lot was created and is waiting for admin approval.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> EditParkingLot(int id)
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId);

            if (parkingLot == null)
            {
                return NotFound();
            }

            var viewModel = new ParkingLotFormViewModel
            {
                Id = parkingLot.Id,
                Name = parkingLot.Name,
                Description = parkingLot.Description,
                City = parkingLot.City,
                Address = parkingLot.Address,
                Latitude = parkingLot.Latitude,
                Longitude = parkingLot.Longitude,
                OpeningTime = parkingLot.OpeningTime,
                ClosingTime = parkingLot.ClosingTime,
                HourlyRate = parkingLot.HourlyRate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParkingLot(ParkingLotFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var ownerId = _userManager.GetUserId(User);
            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(item => item.Id == viewModel.Id && item.OwnerId == ownerId);

            if (parkingLot == null)
            {
                return NotFound();
            }

            parkingLot.Name = viewModel.Name;
            parkingLot.Description = viewModel.Description;
            parkingLot.City = viewModel.City;
            parkingLot.Address = viewModel.Address;
            parkingLot.Latitude = viewModel.Latitude;
            parkingLot.Longitude = viewModel.Longitude;
            parkingLot.OpeningTime = viewModel.OpeningTime;
            parkingLot.ClosingTime = viewModel.ClosingTime;
            parkingLot.HourlyRate = viewModel.HourlyRate;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking lot was updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> ParkingSpots(int parkingLotId)
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingLot = await _context.ParkingLots
                .Include(item => item.ParkingSpots.OrderBy(spot => spot.SpotNumber))
                .FirstOrDefaultAsync(item => item.Id == parkingLotId && item.OwnerId == ownerId);

            if (parkingLot == null)
            {
                return NotFound();
            }

            return View(parkingLot);
        }

        public async Task<IActionResult> CreateParkingSpot(int parkingLotId)
        {
            var parkingLot = await GetOwnerParkingLotAsync(parkingLotId);

            if (parkingLot == null)
            {
                return NotFound();
            }

            return View(new ParkingSpotFormViewModel
            {
                ParkingLotId = parkingLot.Id,
                ParkingLotName = parkingLot.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParkingSpot(ParkingSpotFormViewModel viewModel)
        {
            var parkingLot = await GetOwnerParkingLotAsync(viewModel.ParkingLotId);

            if (parkingLot == null)
            {
                return NotFound();
            }

            if (await _context.ParkingSpots.AnyAsync(spot =>
                    spot.ParkingLotId == viewModel.ParkingLotId &&
                    spot.SpotNumber == viewModel.SpotNumber))
            {
                ModelState.AddModelError(nameof(viewModel.SpotNumber), "This spot number already exists in this parking lot.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.ParkingLotName = parkingLot.Name;
                return View(viewModel);
            }

            var parkingSpot = new ParkingSpot
            {
                ParkingLotId = viewModel.ParkingLotId,
                SpotNumber = viewModel.SpotNumber,
                SpotType = viewModel.SpotType,
                Status = ParkingSpotStatus.Available,
                IsActive = true
            };

            _context.ParkingSpots.Add(parkingSpot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parking spot was created.";
            return RedirectToAction(nameof(ParkingSpots), new { parkingLotId = viewModel.ParkingLotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeSpotStatus(int id, ParkingSpotStatus status)
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingSpot = await _context.ParkingSpots
                .Include(spot => spot.ParkingLot)
                .FirstOrDefaultAsync(spot => spot.Id == id && spot.ParkingLot!.OwnerId == ownerId);

            if (parkingSpot == null)
            {
                return NotFound();
            }

            parkingSpot.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Spot status was updated.";
            return RedirectToAction(nameof(ParkingSpots), new { parkingLotId = parkingSpot.ParkingLotId });
        }

        private async Task<ParkingLot?> GetOwnerParkingLotAsync(int parkingLotId)
        {
            var ownerId = _userManager.GetUserId(User);

            return await _context.ParkingLots
                .FirstOrDefaultAsync(item => item.Id == parkingLotId && item.OwnerId == ownerId);
        }
    }
}
