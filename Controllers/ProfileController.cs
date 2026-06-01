using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;
using ParkingReservation.Models.ViewModels;

namespace ParkingReservation.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Contains(UserRoles.Admin) ? "Admin"
                     : roles.Contains(UserRoles.Owner) ? "Owner"
                     : "User";

            var reservationsCount = await _context.Reservations
                .CountAsync(r => r.UserId == user.Id);

            var vehiclesCount = await _context.Vehicles
                .CountAsync(v => v.UserId == user.Id);

            var parkingLotsCount = role == "Owner"
                ? await _context.ParkingLots.CountAsync(l => l.OwnerId == user.Id)
                : 0;

            var viewModel = new ProfileViewModel
            {
                FullName    = user.FullName,
                Email       = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Address     = user.Address,
                CreatedAt   = user.CreatedAt,
                Role        = role,
                ReservationsCount  = reservationsCount,
                VehiclesCount      = vehiclesCount,
                ParkingLotsCount   = parkingLotsCount
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(new EditProfileViewModel
            {
                FullName    = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address     = user.Address
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName    = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address     = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Refresh the auth cookie so the display name updates
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "تم تحديث بياناتك بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح.";
            return RedirectToAction(nameof(Index));
        }
    }
}
