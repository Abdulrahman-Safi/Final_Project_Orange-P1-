using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard(ParkingLotStatus? status = null)
        {
            var parkingLotsQuery = _context.ParkingLots
                .Include(parkingLot => parkingLot.Owner)
                .Include(parkingLot => parkingLot.ParkingSpots)
                .AsQueryable();
            var owners = await _userManager.GetUsersInRoleAsync(UserRoles.Owner);
            var now = DateTime.Now;
            var lastSixMonths = Enumerable.Range(0, 6)
                .Select(offset => now.AddMonths(-offset))
                .Reverse()
                .ToList();

            var viewModel = new AdminDashboardViewModel
            {
                TotalParkingLotsCount = await parkingLotsQuery.CountAsync(),
                PendingParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Pending),
                ApprovedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Approved),
                RejectedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Rejected),
                SuspendedParkingLotsCount = await parkingLotsQuery.CountAsync(parkingLot => parkingLot.Status == ParkingLotStatus.Suspended),
                TotalUsersCount = await _context.Users.CountAsync(),
                TotalOwnersCount = owners.Count,
                TotalReservationsCount = await _context.Reservations.CountAsync(),
                RecentUsers = await _context.Users.OrderByDescending(user => user.CreatedAt).Take(5).ToListAsync(),
                RecentlyAddedParkingLots = await _context.ParkingLots.Include(lot => lot.Owner).OrderByDescending(lot => lot.CreatedAt).Take(5).ToListAsync(),
                ActivityLogs = new List<string>
                {
                    "تم تحديث إحصائيات النظام.",
                    "تم تحميل قائمة الموافقات المعلقة.",
                    "تم تجهيز مؤشرات المستخدمين والحجوزات."
                },
                NewUsersGrowth = lastSixMonths.Select(month => new DashboardChartItem
                {
                    Label = month.ToString("MM/yyyy"),
                    Value = _context.Users.Count(user => user.CreatedAt.Month == month.Month && user.CreatedAt.Year == month.Year)
                }).ToList(),
                ReservationsStatistics = lastSixMonths.Select(month => new DashboardChartItem
                {
                    Label = month.ToString("MM/yyyy"),
                    Value = _context.Reservations.Count(reservation => reservation.CreatedAt.Month == month.Month && reservation.CreatedAt.Year == month.Year)
                }).ToList(),
                RevenueAnalytics = lastSixMonths.Select(month => new DashboardChartItem
                {
                    Label = month.ToString("MM/yyyy"),
                    Value = _context.Reservations
                        .Where(reservation =>
                            (reservation.Status == ReservationStatus.Confirmed || reservation.Status == ReservationStatus.Completed) &&
                            reservation.CreatedAt.Month == month.Month &&
                            reservation.CreatedAt.Year == month.Year)
                        .Sum(reservation => reservation.TotalPrice)
                }).ToList(),
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

            TempData["SuccessMessage"] = $"تم تغيير حالة المصف إلى {GetParkingLotStatusName(status)}.";
            return RedirectToAction(nameof(ApproveParkingLots));
        }

        public async Task<IActionResult> Users(string? role = null)
        {
            var allUsers = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
            var allWithRoles = new List<(ApplicationUser User, IList<string> Roles)>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                allWithRoles.Add((user, roles));
            }

            // Fixed counts always from the full list
            ViewBag.TotalCount = allWithRoles.Count;
            ViewBag.UserCount  = allWithRoles.Count(x => x.Roles.Contains(UserRoles.User));
            ViewBag.OwnerCount = allWithRoles.Count(x => x.Roles.Contains(UserRoles.Owner));
            ViewBag.AdminCount = allWithRoles.Count(x => x.Roles.Contains(UserRoles.Admin));

            // Filter only for the table
            var usersWithRoles = role == null
                ? allWithRoles
                : allWithRoles.Where(x => x.Roles.Contains(role)).ToList();

            ViewBag.SelectedRole = role;
            return View(usersWithRoles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = user.IsActive ? "تم تفعيل الحساب." : "تم تعطيل الحساب.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string id, string newRole)
        {
            if (newRole != UserRoles.User && newRole != UserRoles.Owner)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, UserRoles.Admin))
            {
                TempData["ErrorMessage"] = "لا يمكن تغيير دور مدير النظام.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["SuccessMessage"] = $"تم تغيير دور المستخدم إلى {(newRole == UserRoles.Owner ? "صاحب مصف" : "مستخدم")}.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> ApproveParkingLots(ParkingLotStatus? status = null)
        {
            var query = _context.ParkingLots
                .Include(l => l.Owner)
                .Include(l => l.ParkingSpots)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            var lots = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var counts = await _context.ParkingLots.GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount   = counts.FirstOrDefault(c => c.Status == ParkingLotStatus.Pending)?.Count ?? 0;
            ViewBag.ApprovedCount  = counts.FirstOrDefault(c => c.Status == ParkingLotStatus.Approved)?.Count ?? 0;
            ViewBag.RejectedCount  = counts.FirstOrDefault(c => c.Status == ParkingLotStatus.Rejected)?.Count ?? 0;
            ViewBag.SuspendedCount = counts.FirstOrDefault(c => c.Status == ParkingLotStatus.Suspended)?.Count ?? 0;

            return View(lots);
        }

        public async Task<IActionResult> Reservations(ReservationStatus? status = null)
        {
            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ParkingSpot)
                    .ThenInclude(s => s!.ParkingLot)
                        .ThenInclude(l => l!.Owner)
                .AsQueryable();

            // Fixed counts from full table (not affected by filter)
            var counts = await _context.Reservations
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.TotalCount     = counts.Sum(c => c.Count);
            ViewBag.PendingCount   = counts.FirstOrDefault(c => c.Status == ReservationStatus.Pending)?.Count   ?? 0;
            ViewBag.ConfirmedCount = counts.FirstOrDefault(c => c.Status == ReservationStatus.Confirmed)?.Count ?? 0;
            ViewBag.CompletedCount = counts.FirstOrDefault(c => c.Status == ReservationStatus.Completed)?.Count ?? 0;
            ViewBag.CancelledCount = counts.FirstOrDefault(c => c.Status == ReservationStatus.Cancelled)?.Count ?? 0;

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var reservations = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(reservations);
        }

        private static string GetParkingLotStatusName(ParkingLotStatus status)
        {
            return status switch
            {
                ParkingLotStatus.Pending => "قيد المراجعة",
                ParkingLotStatus.Approved => "معتمد",
                ParkingLotStatus.Rejected => "مرفوض",
                ParkingLotStatus.Suspended => "معلّق",
                _ => status.ToString()
            };
        }
    }
}
