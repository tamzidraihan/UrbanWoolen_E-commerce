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

        [BindProperty]
        [Required(ErrorMessage = "OTP code is required.")]
        public string OtpCode { get; set; }

        public void OnGet()
        {
            // If session expired, bounce back to Register
            var email = HttpContext.Session.GetString("otp_email");
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Your verification session expired. Please register again.";
                Response.Redirect("./Register");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var otpEmail = HttpContext.Session.GetString("otp_email");
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(otpEmail))
            {
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return Page();
            }

            // ? Defensive: if someone reaches here but the email got registered meanwhile
            var exists = await _userManager.FindByEmailAsync(otpEmail)
                      ?? await _userManager.FindByNameAsync(otpEmail);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "This email is already registered. Please log in.");
                return Page();
            }

            // Get the latest unverified OTP for this email
            var record = _context.EmailOtpVerifications
                .Where(e => e.Email == otpEmail && !e.IsVerified)
                .OrderByDescending(e => e.ExpiryTime)
                .FirstOrDefault();

            if (record == null || record.ExpiryTime < DateTime.UtcNow || record.OtpCode != OtpCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
                return Page();
            }

            // Mark OTP as verified + cleanup other codes for this email
            record.IsVerified = true;
            var others = _context.EmailOtpVerifications.Where(x => x.Email == otpEmail && x.Id != record.Id);
            _context.EmailOtpVerifications.RemoveRange(others);
            await _context.SaveChangesAsync();

            // Retrieve the stored password
            var password = HttpContext.Session.GetString("otp_password");
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Password session expired. Please try registering again.");
                return Page();
            }

            // Create the user now
            var user = new IdentityUser
            {
                UserName = otpEmail,
                Email = otpEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            // Sign in and clear sensitive session values
            HttpContext.Session.Remove("otp_password");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Index");
        }
    }
}
