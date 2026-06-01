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
            var now = DateTime.Now;
            var parkingLots = await _context.ParkingLots
                .Include(parkingLot => parkingLot.ParkingSpots)
                .Where(parkingLot => parkingLot.OwnerId == ownerId)
                .OrderByDescending(parkingLot => parkingLot.CreatedAt)
                .ToListAsync();

            var parkingLotIds = parkingLots.Select(parkingLot => parkingLot.Id).ToList();
            var reservations = await _context.Reservations
                .Include(reservation => reservation.User)
                .Include(reservation => reservation.ParkingSpot)
                    .ThenInclude(spot => spot!.ParkingLot)
                .Where(reservation => parkingLotIds.Contains(reservation.ParkingSpot!.ParkingLotId))
                .OrderByDescending(reservation => reservation.CreatedAt)
                .ToListAsync();
            var activeReservationsCount = await _context.Reservations
                .CountAsync(reservation =>
                    parkingLotIds.Contains(reservation.ParkingSpot!.ParkingLotId) &&
                    (reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed));
            var lastSevenDays = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-offset))
                .Reverse()
                .ToList();
            var lastSixMonths = Enumerable.Range(0, 6)
                .Select(offset => now.AddMonths(-offset))
                .Reverse()
                .ToList();

            var viewModel = new OwnerDashboardViewModel
            {
                ParkingLots = parkingLots,
                ParkingLotsCount = parkingLots.Count,
                ParkingSpotsCount = parkingLots.Sum(parkingLot => parkingLot.ParkingSpots.Count),
                AvailableSpotsCount = parkingLots.Sum(parkingLot => parkingLot.ParkingSpots.Count(spot => spot.Status == ParkingSpotStatus.Available)),
                ActiveReservationsCount = activeReservationsCount,
                MonthlyRevenue = reservations
                    .Where(reservation =>
                        (reservation.Status == ReservationStatus.Confirmed || reservation.Status == ReservationStatus.Completed) &&
                        reservation.CreatedAt.Month == now.Month &&
                        reservation.CreatedAt.Year == now.Year)
                    .Sum(reservation => reservation.TotalPrice),
                RecentReservations = reservations.Take(6).ToList(),
                MostBookedParkingLots = parkingLots
                    .OrderByDescending(lot => reservations.Count(reservation => reservation.ParkingSpot?.ParkingLotId == lot.Id))
                    .Take(4)
                    .ToList(),
                ReservationsPerDay = lastSevenDays.Select(day => new DashboardChartItem
                {
                    Label = day.ToString("MM/dd"),
                    Value = reservations.Count(reservation => reservation.CreatedAt.Date == day.Date)
                }).ToList(),
                RevenuePerMonth = lastSixMonths.Select(month => new DashboardChartItem
                {
                    Label = month.ToString("MM/yyyy"),
                    Value = reservations
                        .Where(reservation =>
                            (reservation.Status == ReservationStatus.Confirmed || reservation.Status == ReservationStatus.Completed) &&
                            reservation.CreatedAt.Month == month.Month &&
                            reservation.CreatedAt.Year == month.Year)
                        .Sum(reservation => reservation.TotalPrice)
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ManageParkingLots()
        {
            var ownerId = _userManager.GetUserId(User);
            var lots = await _context.ParkingLots
                .Include(l => l.ParkingSpots)
                .Where(l => l.OwnerId == ownerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(lots);
        }

        public IActionResult CreateParkingLot()
        {
            ViewBag.AmmanAreas = AmmanAreas.All;
            return View(new ParkingLotFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParkingLot(ParkingLotFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AmmanAreas = AmmanAreas.All;
                return View(viewModel);
            }

            var ownerId = _userManager.GetUserId(User);

            var parkingLot = new ParkingLot
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                City = viewModel.City,
                Area = string.IsNullOrWhiteSpace(viewModel.Area) ? null : viewModel.Area,
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

            TempData["SuccessMessage"] = "تم إنشاء المصف وهو بانتظار موافقة الإدارة.";
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
                Area = parkingLot.Area,
                Address = parkingLot.Address,
                Latitude = parkingLot.Latitude,
                Longitude = parkingLot.Longitude,
                OpeningTime = parkingLot.OpeningTime,
                ClosingTime = parkingLot.ClosingTime,
                HourlyRate = parkingLot.HourlyRate
            };

            ViewBag.AmmanAreas = AmmanAreas.All;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParkingLot(ParkingLotFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AmmanAreas = AmmanAreas.All;
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
            parkingLot.Area = string.IsNullOrWhiteSpace(viewModel.Area) ? null : viewModel.Area;
            parkingLot.Address = viewModel.Address;
            parkingLot.Latitude = viewModel.Latitude;
            parkingLot.Longitude = viewModel.Longitude;
            parkingLot.OpeningTime = viewModel.OpeningTime;
            parkingLot.ClosingTime = viewModel.ClosingTime;
            parkingLot.HourlyRate = viewModel.HourlyRate;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث بيانات المصف.";
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
                ModelState.AddModelError(nameof(viewModel.SpotNumber), "رقم الموقف موجود مسبقاً في هذا المصف.");
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

            TempData["SuccessMessage"] = "تم إنشاء الموقف.";
            return RedirectToAction(nameof(ParkingSpots), new { parkingLotId = viewModel.ParkingLotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpot(int id, ParkingSpotStatus status, ParkingSpotType spotType)
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingSpot = await _context.ParkingSpots
                .Include(spot => spot.ParkingLot)
                .FirstOrDefaultAsync(spot => spot.Id == id && spot.ParkingLot!.OwnerId == ownerId);

            if (parkingSpot == null) return NotFound();

            parkingSpot.Status   = status;
            parkingSpot.SpotType = spotType;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث الموقف.";
            return RedirectToAction(nameof(ParkingSpots), new { parkingLotId = parkingSpot.ParkingLotId });
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

            TempData["SuccessMessage"] = "تم تحديث حالة الموقف.";
            return RedirectToAction(nameof(ParkingSpots), new { parkingLotId = parkingSpot.ParkingLotId });
        }

        public async Task<IActionResult> Reservations()
        {
            var ownerId = _userManager.GetUserId(User);
            var reservations = await _context.Reservations
                .Include(reservation => reservation.User)
                .Include(reservation => reservation.ParkingSpot)
                    .ThenInclude(spot => spot!.ParkingLot)
                .Include(reservation => reservation.Vehicle)
                .Include(reservation => reservation.Payment)
                .Where(reservation => reservation.ParkingSpot!.ParkingLot!.OwnerId == ownerId)
                .OrderByDescending(reservation => reservation.CreatedAt)
                .ToListAsync();

            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReservation(int id)
        {
            var ownerId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(item => item.ParkingSpot)
                    .ThenInclude(spot => spot!.ParkingLot)
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.Status == ReservationStatus.Pending &&
                    item.ParkingSpot!.ParkingLot!.OwnerId == ownerId);

            if (reservation == null)
            {
                return NotFound();
            }

            var hasConflict = await _context.Reservations.AnyAsync(item =>
                item.Id != reservation.Id &&
                item.ParkingSpotId == reservation.ParkingSpotId &&
                item.Status == ReservationStatus.Confirmed &&
                reservation.StartTime < item.EndTime &&
                reservation.EndTime > item.StartTime);

            if (hasConflict)
            {
                TempData["ErrorMessage"] = "لا يمكن الموافقة على الحجز لوجود حجز مؤكد بنفس الفترة.";
                return RedirectToAction(nameof(Reservations));
            }

            reservation.Status = ReservationStatus.Confirmed;

            // If the reservation period is currently active, mark the spot as Reserved immediately
            var now = DateTime.Now;
            if (reservation.StartTime <= now && reservation.EndTime > now && reservation.ParkingSpot != null)
                reservation.ParkingSpot.Status = ParkingSpotStatus.Reserved;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت الموافقة على الحجز.";
            return RedirectToAction(nameof(Reservations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReservation(int id)
        {
            var ownerId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(item => item.ParkingSpot)
                    .ThenInclude(spot => spot!.ParkingLot)
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.Status == ReservationStatus.Pending &&
                    item.ParkingSpot!.ParkingLot!.OwnerId == ownerId);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم رفض الحجز.";
            return RedirectToAction(nameof(Reservations));
        }

        public async Task<IActionResult> Revenue()
        {
            var ownerId = _userManager.GetUserId(User);
            var parkingLotIds = await _context.ParkingLots
                .Where(l => l.OwnerId == ownerId)
                .Select(l => l.Id)
                .ToListAsync();

            var reservations = await _context.Reservations
                .Include(r => r.ParkingSpot)
                    .ThenInclude(s => s!.ParkingLot)
                .Where(r =>
                    parkingLotIds.Contains(r.ParkingSpot!.ParkingLotId) &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var now = DateTime.Now;
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Reverse()
                .ToList();

            ViewBag.RevenuePerMonth = last12Months.Select(m => new DashboardChartItem
            {
                Label = m.ToString("MM/yyyy"),
                Value = reservations
                    .Where(r => r.CreatedAt.Month == m.Month && r.CreatedAt.Year == m.Year)
                    .Sum(r => r.TotalPrice)
            }).ToList();

            ViewBag.TotalRevenue = reservations.Sum(r => r.TotalPrice);
            ViewBag.ThisMonthRevenue = reservations
                .Where(r => r.CreatedAt.Month == now.Month && r.CreatedAt.Year == now.Year)
                .Sum(r => r.TotalPrice);
            ViewBag.TotalPaid = reservations.Count;

            ViewBag.RevenuePerLot = await _context.ParkingLots
                .Where(l => l.OwnerId == ownerId)
                .Select(l => new
                {
                    l.Name,
                    Revenue = l.ParkingSpots
                        .SelectMany(s => s.Reservations)
                        .Where(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed)
                        .Sum(r => r.TotalPrice),
                    Count = l.ParkingSpots
                        .SelectMany(s => s.Reservations)
                        .Count(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return View(reservations);
        }

        private async Task<ParkingLot?> GetOwnerParkingLotAsync(int parkingLotId)
        {
            var ownerId = _userManager.GetUserId(User);

            return await _context.ParkingLots
                .FirstOrDefaultAsync(item => item.Id == parkingLotId && item.OwnerId == ownerId);
        }
    }
}
