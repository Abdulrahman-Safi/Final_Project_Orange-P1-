using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Controllers
{
    [Authorize]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VehiclesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.PlateNumber)
                .ToListAsync();
            return View(vehicles);
        }

        public IActionResult Create(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new Vehicle());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle vehicle, string? returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);

            // UserId is set by the controller, not the form — remove it from validation
            ModelState.Remove(nameof(vehicle.UserId));

            var duplicate = await _context.Vehicles
                .AnyAsync(v => v.UserId == userId && v.PlateNumber == vehicle.PlateNumber);

            if (duplicate)
                ModelState.AddModelError(nameof(vehicle.PlateNumber), "هذه اللوحة مسجلة مسبقاً.");

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(vehicle);
            }

            vehicle.UserId = userId!;
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إضافة المركبة بنجاح. يمكنك الآن إتمام الحجز.";

            // Return to the reservation page if redirected from there
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Vehicle vehicle)
        {
            var userId = _userManager.GetUserId(User);

            // UserId is set by the controller, not the form
            ModelState.Remove(nameof(vehicle.UserId));

            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicle.Id && v.UserId == userId);

            if (existing == null) return NotFound();

            var duplicate = await _context.Vehicles
                .AnyAsync(v => v.UserId == userId && v.PlateNumber == vehicle.PlateNumber && v.Id != vehicle.Id);

            if (duplicate)
                ModelState.AddModelError(nameof(vehicle.PlateNumber), "هذه اللوحة مسجلة مسبقاً.");

            if (!ModelState.IsValid)
                return View(vehicle);

            existing.PlateNumber = vehicle.PlateNumber;
            existing.Make = vehicle.Make;
            existing.Model = vehicle.Model;
            existing.Color = vehicle.Color;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم تحديث المركبة.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (vehicle == null) return NotFound();

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف المركبة.";
            return RedirectToAction(nameof(Index));
        }
    }
}
