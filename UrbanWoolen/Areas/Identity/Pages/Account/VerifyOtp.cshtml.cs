using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using UrbanWoolen.Data;
using UrbanWoolen.Models;

namespace UrbanWoolen.Areas.Identity.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        private const string RegisterEmailKey = "otp_email";
        private const string RegisterPasswordKey = "otp_password";
        private const string ResetEmailKey = "reset_email";
        private const string ResetVerifiedKey = "reset_verified";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public VerifyOtpModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Decide flow by query (?mode=reset) or by session presence
        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; } = "register";

        [BindProperty]
        [Required(ErrorMessage = "OTP code is required.")]
        public string OtpCode { get; set; }

        public IActionResult OnGet()
        {
            var isReset = IsResetFlow();
            var email = isReset
                ? HttpContext.Session.GetString(ResetEmailKey)
                : HttpContext.Session.GetString(RegisterEmailKey);

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = isReset
                    ? "Your reset session expired. Please start again."
                    : "Your verification session expired. Please register again.";

                return isReset ? RedirectToPage("./ForgotPassword") : RedirectToPage("./Register");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var isReset = IsResetFlow();
            var email = isReset
                ? HttpContext.Session.GetString(ResetEmailKey)
                : HttpContext.Session.GetString(RegisterEmailKey);

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return Page();
            }

            if (isReset)
            {
                // ------------ RESET FLOW ------------
                var user = await _userManager.FindByEmailAsync(email)
                           ?? await _userManager.FindByNameAsync(email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "No account found with this email.");
                    return Page();
                }

                var record = _context.EmailOtpVerifications
                    .Where(e => e.Email == email && !e.IsVerified)
                    .OrderByDescending(e => e.ExpiryTime)
                    .FirstOrDefault();

                if (record == null || record.ExpiryTime < DateTime.UtcNow || record.OtpCode != OtpCode)
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
                    return Page();
                }

                record.IsVerified = true;

                // Clean up other codes for this email
                var others = _context.EmailOtpVerifications.Where(x => x.Email == email && x.Id != record.Id);
                _context.EmailOtpVerifications.RemoveRange(others);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString(ResetVerifiedKey, "true");
                return RedirectToPage("./ResetPassword");
            }
            else
            {
                // --------- REGISTRATION FLOW (your original logic) ---------
                var exists = await _userManager.FindByEmailAsync(email)
                            ?? await _userManager.FindByNameAsync(email);
                if (exists != null)
                {
                    ModelState.AddModelError(string.Empty, "This email is already registered. Please log in.");
                    return Page();
                }

                var record = _context.EmailOtpVerifications
                    .Where(e => e.Email == email && !e.IsVerified)
                    .OrderByDescending(e => e.ExpiryTime)
                    .FirstOrDefault();

                if (record == null || record.ExpiryTime < DateTime.UtcNow || record.OtpCode != OtpCode)
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
                    return Page();
                }

                record.IsVerified = true;
                var others = _context.EmailOtpVerifications.Where(x => x.Email == email && x.Id != record.Id);
                _context.EmailOtpVerifications.RemoveRange(others);
                await _context.SaveChangesAsync();

                var password = HttpContext.Session.GetString(RegisterPasswordKey);
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError(string.Empty, "Password session expired. Please try registering again.");
                    return Page();
                }

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return Page();
                }

                HttpContext.Session.Remove(RegisterPasswordKey);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Index");
            }
        }

        private bool IsResetFlow()
        {
            if (string.Equals(Mode, "reset", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(Mode, "register", StringComparison.OrdinalIgnoreCase)) return false;

            // Fallback: presence of reset_email session
            return !string.IsNullOrEmpty(HttpContext.Session.GetString(ResetEmailKey));
        }
    }
}
