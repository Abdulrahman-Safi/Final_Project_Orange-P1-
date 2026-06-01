using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;
using ParkingReservation.Models.ViewModels;
using System.Diagnostics;

namespace ParkingReservation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (User.IsInRole(UserRoles.Owner))
            {
                return RedirectToAction("Dashboard", "Owner");
            }

            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction(nameof(UserHome));
            }

            return View();
        }

        [Authorize(Roles = UserRoles.User)]
        public async Task<IActionResult> UserHome()
        {
            var userId = _userManager.GetUserId(User);
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.UserFullName = currentUser?.FullName ?? "";
            var approvedLots = await _context.ParkingLots
                .Include(parkingLot => parkingLot.ParkingSpots)
                .Where(parkingLot => parkingLot.IsActive && parkingLot.Status == ParkingLotStatus.Approved)
                .OrderBy(parkingLot => parkingLot.City)
                .ThenBy(parkingLot => parkingLot.Name)
                .Take(6)
                .ToListAsync();

            var reservations = await _context.Reservations
                .Include(reservation => reservation.ParkingSpot)
                    .ThenInclude(spot => spot!.ParkingLot)
                .Where(reservation => reservation.UserId == userId)
                .OrderByDescending(reservation => reservation.CreatedAt)
                .ToListAsync();

            var viewModel = new UserHomeViewModel
            {
                NearbyParkingLots = approvedLots,
                FavoriteParkingLots = approvedLots.Take(3).ToList(),
                ActiveReservations = reservations
                    .Where(reservation => reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed)
                    .Take(5)
                    .ToList(),
                ReservationHistory = reservations
                    .Where(reservation => reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Completed)
                    .Take(5)
                    .ToList(),
                AvailableSpotsCount = approvedLots.Sum(parkingLot => parkingLot.ParkingSpots.Count(spot =>
                    spot.IsActive && spot.Status == ParkingSpotStatus.Available))
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ContactSubmit(string name, string email, string subject, string message)
        {
            TempData["SuccessMessage"] = "شكراً لتواصلك معنا! سنرد عليك في أقرب وقت ممكن.";
            return RedirectToAction(nameof(Contact));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
