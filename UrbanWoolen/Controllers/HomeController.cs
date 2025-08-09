using Microsoft.AspNetCore.Mvc;
using UrbanWoolen.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
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

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

