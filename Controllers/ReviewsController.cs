using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int parkingLotId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "التقييم يجب أن يكون بين 1 و 5.";
                return RedirectToAction("Details", "ParkingLots", new { id = parkingLotId });
            }

            var userId = _userManager.GetUserId(User);

            // Check user has a completed/confirmed reservation at this lot
            var hasReservation = await _context.Reservations
                .AnyAsync(r =>
                    r.UserId == userId &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed) &&
                    r.ParkingSpot!.ParkingLotId == parkingLotId);

            if (!hasReservation)
            {
                TempData["ErrorMessage"] = "يجب أن يكون لديك حجز مؤكد في هذا المصف لتتمكن من التقييم.";
                return RedirectToAction("Details", "ParkingLots", new { id = parkingLotId });
            }

            // One review per user per lot
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ParkingLotId == parkingLotId);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.CreatedAt = DateTime.Now;
            }
            else
            {
                _context.Reviews.Add(new Review
                {
                    UserId = userId!,
                    ParkingLotId = parkingLotId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم حفظ تقييمك بنجاح.";
            return RedirectToAction("Details", "ParkingLots", new { id = parkingLotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int parkingLotId)
        {
            var userId = _userManager.GetUserId(User);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف تقييمك.";
            return RedirectToAction("Details", "ParkingLots", new { id = parkingLotId });
        }
    }
}
