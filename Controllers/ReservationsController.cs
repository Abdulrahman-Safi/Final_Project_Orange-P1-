using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingReservation.Data;
using ParkingReservation.Models;

namespace ParkingReservation.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReservationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int parkingSpotId, DateTime? startTime, DateTime? endTime)
        {
            var parkingSpot = await GetReservableSpotAsync(parkingSpotId);
            if (parkingSpot == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.PlateNumber)
                .ToListAsync();

            // No vehicles registered — redirect to add one first
            if (!vehicles.Any())
            {
                TempData["ErrorMessage"] = "يجب إضافة مركبة واحدة على الأقل قبل إتمام الحجز.";
                var returnUrl = Url.Action("Create", "Reservations", new { parkingSpotId, startTime, endTime });
                return RedirectToAction("Create", "Vehicles", new { returnUrl });
            }

            ViewBag.DefaultStartTime = (startTime ?? DateTime.Now.AddHours(1)).ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DefaultEndTime   = (endTime   ?? DateTime.Now.AddHours(2)).ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Vehicles = vehicles;

            return View(parkingSpot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int parkingSpotId, DateTime startTime, DateTime endTime, int? vehicleId)
        {
            var parkingSpot = await GetReservableSpotAsync(parkingSpotId);

            if (parkingSpot == null)
            {
                return NotFound();
            }

            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "وقت الانتهاء يجب أن يكون بعد وقت البداية.");
            }

            if (startTime < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "لا يمكن إنشاء حجز في وقت سابق للوقت الحالي.");
            }

            var hasConflict = await _context.Reservations.AnyAsync(reservation =>
                reservation.ParkingSpotId == parkingSpotId &&
                reservation.Status != ReservationStatus.Cancelled &&
                startTime < reservation.EndTime &&
                endTime > reservation.StartTime);

            if (hasConflict)
            {
                ModelState.AddModelError(string.Empty, "هذا الموقف غير متاح خلال الفترة المحددة.");
            }

            var userId = _userManager.GetUserId(User);

            // Vehicle is required
            if (!vehicleId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "يجب اختيار مركبة لإتمام الحجز.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DefaultStartTime = startTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DefaultEndTime   = endTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.Vehicles = await _context.Vehicles
                    .Where(v => v.UserId == userId)
                    .OrderBy(v => v.PlateNumber)
                    .ToListAsync();
                return View(parkingSpot);
            }

            var durationHours = Math.Ceiling((endTime - startTime).TotalHours);
            var totalPrice = (decimal)durationHours * parkingSpot.ParkingLot!.HourlyRate;

            // Validate vehicleId belongs to the current user
            var resolvedVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId!.Value && v.UserId == userId);

            if (resolvedVehicle == null)
            {
                ModelState.AddModelError(string.Empty, "المركبة المختارة غير صالحة.");
                ViewBag.DefaultStartTime = startTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DefaultEndTime   = endTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.Vehicles = await _context.Vehicles
                    .Where(v => v.UserId == userId)
                    .OrderBy(v => v.PlateNumber)
                    .ToListAsync();
                return View(parkingSpot);
            }

            int? resolvedVehicleId = resolvedVehicle.Id;

            var reservation = new Reservation
            {
                UserId = userId!,
                ParkingSpotId = parkingSpot.Id,
                StartTime = startTime,
                EndTime = endTime,
                TotalPrice = totalPrice,
                VehicleId = resolvedVehicleId,
                Status = ReservationStatus.Confirmed
            };

            // Mark the spot as Reserved immediately so it disappears from listings
            parkingSpot.Status = ParkingSpotStatus.Reserved;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction("Pay", "Payments", new { reservationId = reservation.Id });
        }

        public async Task<IActionResult> MyReservations()
        {
            var userId = _userManager.GetUserId(User);
            var reservations = await _context.Reservations
                .Include(r => r.ParkingSpot)
                    .ThenInclude(s => s!.ParkingLot)
                .Include(r => r.Vehicle)
                .Include(r => r.Payment)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == userId &&
                    (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed));

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء هذا الحجز.";
                return RedirectToAction(nameof(MyReservations));
            }

            reservation.Status = ReservationStatus.Cancelled;

            // If already paid, mark payment as refunded
            if (reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Paid)
            {
                reservation.Payment.Status = PaymentStatus.Refunded;
                TempData["SuccessMessage"] = "تم إلغاء الحجز وسيتم استرداد المبلغ المدفوع.";
            }
            else
            {
                TempData["SuccessMessage"] = "تم إلغاء الحجز بنجاح.";
            }

            // Free the spot if no other confirmed reservation exists for it
            var hasOtherConfirmed = await _context.Reservations.AnyAsync(r =>
                r.Id != reservation.Id &&
                r.ParkingSpotId == reservation.ParkingSpotId &&
                r.Status == ReservationStatus.Confirmed);

            if (!hasOtherConfirmed)
            {
                var spot = await _context.ParkingSpots.FindAsync(reservation.ParkingSpotId);
                if (spot != null)
                    spot.Status = ParkingSpotStatus.Available;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyReservations));
        }

        private async Task<ParkingSpot?> GetReservableSpotAsync(int parkingSpotId)
        {
            return await _context.ParkingSpots
                .Include(spot => spot.ParkingLot)
                    .ThenInclude(lot => lot!.Owner)
                .FirstOrDefaultAsync(spot =>
                    spot.Id == parkingSpotId &&
                    spot.IsActive &&
                    spot.Status == ParkingSpotStatus.Available &&
                    spot.ParkingLot != null &&
                    spot.ParkingLot.IsActive &&
                    spot.ParkingLot.Status == ParkingLotStatus.Approved &&
                    spot.ParkingLot.Owner != null &&
                    spot.ParkingLot.Owner.IsActive &&
                    // Reject spots currently occupied by an active confirmed reservation
                    !spot.Reservations.Any(r =>
                        r.Status == ReservationStatus.Confirmed &&
                        r.StartTime <= DateTime.Now &&
                        r.EndTime > DateTime.Now));
        }
    }
}
