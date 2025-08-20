using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace UrbanWoolen.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            // ❗ 1) STOP EARLY if email is already registered
            var existingByEmail = await _userManager.FindByEmailAsync(Input.Email);
            var existingByName = await _userManager.FindByNameAsync(Input.Email); // in case you use Email as UserName
            if (existingByEmail != null || existingByName != null)
            {
                ModelState.AddModelError("Input.Email", "This email is already registered. Please log in or reset your password.");
                return Page(); // ⟵ do NOT generate/send OTP; stay on Register
            }

            // 2) Generate OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            var minutes = int.TryParse(_configuration["Email:OtpValidityMinutes"], out var m) ? m : 10;
            var expiry = DateTime.UtcNow.AddMinutes(minutes);

            // 3) (Hygiene) Remove older unverified OTPs for this email
            var olds = _context.EmailOtpVerifications.Where(x => x.Email == Input.Email && !x.IsVerified);
            _context.EmailOtpVerifications.RemoveRange(olds);

            // 4) Save OTP
            var otpRecord = new EmailOtpVerification
            {
                Email = Input.Email,
                OtpCode = otpCode,
                ExpiryTime = expiry,
                IsVerified = false
            };
            _context.EmailOtpVerifications.Add(otpRecord);
            await _context.SaveChangesAsync();

            // 5) Send OTP
            await _emailSender.SendEmailAsync(
                Input.Email,
                "UrbanWoolen - Verify Your Email",
                $"Your verification OTP is <strong>{otpCode}</strong>. It will expire in {minutes} minutes.");

            // 6) Stash email + password in session for Verify step
            HttpContext.Session.SetString("otp_email", Input.Email);
            HttpContext.Session.SetString("otp_password", Input.Password);

            return RedirectToPage("VerifyOtp");
        }


        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
