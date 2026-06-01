using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParkingReservation.Models;

namespace ParkingReservation.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
            [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "تذكرني")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["ErrorMessage"]))
            {
                ModelState.AddModelError(string.Empty, HttpContext.Request.Query["ErrorMessage"]!);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                // Respect returnUrl if it was set by [Authorize] redirect
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl != Url.Content("~/") && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);

                if (user != null && await _signInManager.UserManager.IsInRoleAsync(user, UserRoles.Admin))
                {
                    return RedirectToAction("Dashboard", "Admin", new { area = "" });
                }

                if (user != null && await _signInManager.UserManager.IsInRoleAsync(user, UserRoles.Owner))
                {
                    return RedirectToAction("Dashboard", "Owner", new { area = "" });
                }

                return RedirectToAction("UserHome", "Home", new { area = "" });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "تم قفل الحساب مؤقتاً.");
                return Page();
            }

            if (result.IsNotAllowed)
            {
                var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                if (user != null && !user.IsActive)
                    ModelState.AddModelError(string.Empty, "هذا الحساب معطّل. يرجى التواصل مع الإدارة.");
                else
                    ModelState.AddModelError(string.Empty, "تسجيل الدخول غير مسموح به.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "محاولة تسجيل الدخول غير صحيحة.");
            return Page();
        }
    }
}
