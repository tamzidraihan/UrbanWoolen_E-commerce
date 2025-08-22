using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UrbanWoolen.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ResetPasswordModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string Password { get; set; } = default!;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = default!;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            var email = HttpContext.Session.GetString("reset_email");
            var verified = HttpContext.Session.GetString("reset_verified");
            if (string.IsNullOrWhiteSpace(email) || verified != "true")
            {
                TempData["Error"] = "Your reset session expired. Please start again.";
                return RedirectToPage("./ForgotPassword");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = HttpContext.Session.GetString("reset_email");
            var verified = HttpContext.Session.GetString("reset_verified");
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(email) || verified != "true")
                return Page();

            var user = await _userManager.FindByEmailAsync(email)
                       ?? await _userManager.FindByNameAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, Input.Password);

            if (!reset.Succeeded)
            {
                foreach (var e in reset.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            // Clear session and sign in
            HttpContext.Session.Remove("reset_email");
            HttpContext.Session.Remove("reset_verified");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Index");
        }
    }
}
