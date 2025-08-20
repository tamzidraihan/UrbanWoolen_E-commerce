using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using UrbanWoolen.Models.ViewModels;

namespace UrbanWoolen.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StoreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // /Store
        public async Task<IActionResult> Index(string category, string search, decimal? minPrice, decimal? maxPrice)
        {
            var products = await _context.Products.ToListAsync();

            if (!string.IsNullOrEmpty(category))
            {
                ViewBag.SelectedCategory = category;
                products = products.Where(p => p.Category.ToString() == category).ToList();
            }
            else
            {
                ViewBag.SelectedCategory = null;
            }

            if (!string.IsNullOrEmpty(search))
            {
                ViewBag.SearchTerm = search;
                products = products.Where(p => p.Name.ToLower().Contains(search.ToLower())).ToList();
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value).ToList();
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value).ToList();
            }

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products);
        }

        // /Store/Details/5?region=BD&unit=cm
        public async Task<IActionResult> Details(int id, string? region = null, string? unit = "cm")
        {
            // Keep: product + reviews
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Find available regions for this product's category
            var regions = await _context.SizeCharts
                .Where(c => c.Category == product.Category)
                .Select(c => c.Region)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            // Pick selected region: querystring > BD (if available) > first
            var selectedRegion = string.IsNullOrWhiteSpace(region)
                ? (regions.Contains("BD") ? "BD" : regions.FirstOrDefault())
                : region;

            UrbanWoolen.Models.SizeChart? chart = null;
            if (!string.IsNullOrWhiteSpace(selectedRegion))
            {
                chart = await _context.SizeCharts
                    .Include(c => c.Items)
                    .Where(c => c.Category == product.Category && c.Region == selectedRegion)
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();
            }

            // Pass extras via ViewBag so the view can stay @model Product
            ViewBag.AvailableRegions = regions;                    // List<string>
            ViewBag.SelectedRegion = selectedRegion;               // string?
            ViewBag.SelectedUnit = unit == "in" ? "in" : "cm";     // "cm" | "in"
            ViewBag.SizeChart = chart;                             // SizeChart or null

            return View(product);  // IMPORTANT: still returning Product as the model
        }

    }
}
