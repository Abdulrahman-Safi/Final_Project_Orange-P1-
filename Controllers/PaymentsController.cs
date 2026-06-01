using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Pay(int reservationId)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(r => r.ParkingSpot)
                    .ThenInclude(s => s!.ParkingLot)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r =>
                    r.Id == reservationId &&
                    r.UserId == userId &&
                    r.Status == ReservationStatus.Confirmed &&
                    r.Payment == null);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int reservationId, PaymentMethod method)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r =>
                    r.Id == reservationId &&
                    r.UserId == userId &&
                    r.Status == ReservationStatus.Confirmed &&
                    r.Payment == null);

            if (reservation == null)
                return NotFound();

            // Simulate payment — always succeeds
            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalPrice,
                Method = method,
                Status = PaymentStatus.Paid,
                TransactionId = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PaymentSuccess), new { transactionId = payment.TransactionId, amount = payment.Amount, method = (int)payment.Method });
        }
        public IActionResult PaymentSuccess(string transactionId, decimal amount, int method)
        {
            ViewBag.TransactionId = transactionId;
            ViewBag.Amount = amount;
            ViewBag.Method = (PaymentMethod)method;
            ViewBag.PaidAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            return View();
        }
    }
}
