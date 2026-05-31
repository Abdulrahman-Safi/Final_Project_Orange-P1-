using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParkingReservation.Models;

namespace ParkingReservation.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
            [StringLength(100)]
            [Display(Name = "الاسم الكامل")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
            [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح.")]
            [Display(Name = "البريد الإلكتروني")]
            public string Email { get; set; } = string.Empty;

            [Phone(ErrorMessage = "رقم الهاتف غير صحيح.")]
            [Display(Name = "رقم الهاتف")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "نوع الحساب مطلوب.")]
            [Display(Name = "نوع الحساب")]
            public string Role { get; set; } = UserRoles.User;

            [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
            [StringLength(100, ErrorMessage = "{0} يجب أن تكون بين {2} و {1} أحرف.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "تأكيد كلمة المرور")]
            [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (Input.Role != UserRoles.User && Input.Role != UserRoles.Owner)
            {
                ModelState.AddModelError("Input.Role", "يرجى اختيار نوع حساب صحيح.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                PhoneNumber = Input.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _userManager.AddToRoleAsync(user, Input.Role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
