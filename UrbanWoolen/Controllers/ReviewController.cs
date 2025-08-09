using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UrbanWoolen.Data;
using UrbanWoolen.Models;

namespace UrbanWoolen.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Add(Review review)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);

                review.UserId = userId;
                review.UserName = user?.UserName ?? "Guest";

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Store", new { id = review.ProductId });
            }

            // Optional: Show ModelState errors for debugging
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine("❌ ModelState Error: " + error.ErrorMessage);
            }

            return BadRequest("Invalid review data");
        }

    }
}
