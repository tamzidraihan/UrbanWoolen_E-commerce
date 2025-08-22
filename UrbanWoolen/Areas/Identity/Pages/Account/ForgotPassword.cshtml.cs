using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UrbanWoolen.Data;
using UrbanWoolen.Models;

namespace UrbanWoolen.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = default!;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var email = (Input.Email ?? string.Empty).Trim();

            // Must exist for reset
            var user = await _userManager.FindByEmailAsync(email)
                       ?? await _userManager.FindByNameAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No account found with this email.");
                return Page();
            }

            // Create a 6-digit code
            var otp = GenerateNumericOtp(6);

            // Remove any prior OTPs for this email (keeps only one active)
            var prior = _context.EmailOtpVerifications.Where(e => e.Email == email);
            _context.EmailOtpVerifications.RemoveRange(prior);

            // Save new code (10 minutes expiry)
            _context.EmailOtpVerifications.Add(new EmailOtpVerification
            {
                Email = email,
                OtpCode = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                IsVerified = false
            });

            await _context.SaveChangesAsync();

            // Send email
            await _emailSender.SendEmailAsync(
                email,
                "Your password reset code",
                $@"<p>Use this code to reset your password:</p>
                   <p style='font-size:18px'><b>{otp}</b></p>
                   <p>This code expires in 10 minutes.</p>");

            // Put email in session for the verify flow
            HttpContext.Session.SetString("reset_email", email);

            // Reuse the same VerifyOtp page, just tell it we're in reset mode
            return RedirectToPage("./VerifyOtp", new { mode = "reset" });
        }

        private static string GenerateNumericOtp(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(length);
            foreach (var b in bytes) sb.Append((b % 10).ToString());
            return sb.ToString();
        }
    }
}
