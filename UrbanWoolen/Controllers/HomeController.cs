using Microsoft.AspNetCore.Mvc;
using UrbanWoolen.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UrbanWoolen.Models.ViewModels;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Existing featured sections
        var hotCollection = await _context.Products
            .OrderByDescending(p =>
                _context.OrderItems.Where(o => o.ProductId == p.Id).Sum(o => (int?)o.Quantity) ?? 0)
            .Take(4)
            .ToListAsync();

        var winterCollection = await _context.Products
            .Where(p => p.IsWinterCollection)
            .Take(4)
            .ToListAsync();

        ViewBag.HotCollection = hotCollection;
        ViewBag.WinterCollection = winterCollection;

        // NEW: New Arrivals (latest added products). If you later add CreatedAt, switch to OrderByDescending(p => p.CreatedAt)
        var newArrivals = await _context.Products
            .OrderByDescending(p => p.Id)
            .Take(8)
            .ToListAsync();

        // Discounts
        var now = DateTime.UtcNow;
        var discounts = await _context.Discounts
            .Include(d => d.Product)
            .Where(d => d.Product.Stock > 0 &&
                        d.Product.Price > 0 &&
                        d.StartsAt <= now &&
                        (d.EndsAt == null || d.EndsAt >= now))
            .OrderByDescending(d => d.Type)
            .Take(12)
            .Select(d => new DiscountProductVM { Product = d.Product, Discount = d })
            .ToListAsync();

        var vm = new HomeViewModel
        {
            ActiveDiscounts = discounts,
            NewArrivals = newArrivals
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
