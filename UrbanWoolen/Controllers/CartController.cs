// ✅ Updated CartController.cs
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UrbanWoolen.Models.ViewModels;

namespace UrbanWoolen.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string SessionKey = "cart";
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCart()
        {
            var sessionCart = HttpContext.Session.GetString(SessionKey);
            return string.IsNullOrEmpty(sessionCart)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult AddToCart(int id, string returnUrl = null)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }

            SaveCart(cart);
            TempData["CartMessage"] = "Product added to cart!";

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["CartMessage"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart
            };

            return View("Checkout", model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            model.CartItems = cart;

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var order = new Order
            {
                UserId = _userManager.GetUserId(User),
                Items = cart.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    ProductName = c.Name,
                    Quantity = c.Quantity,
                    Price = c.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var sslService = HttpContext.RequestServices.GetRequiredService<SslCommerzService>();
            var redirectUrl = await sslService.InitiatePaymentAsync(order,
                successUrl: Url.Action("Success", "Payment", null, Request.Scheme),
                failUrl: Url.Action("Fail", "Payment", null, Request.Scheme),
                cancelUrl: Url.Action("Cancel", "Payment", null, Request.Scheme),
                customer: model);

            return Redirect(redirectUrl);
        }
    }
}
