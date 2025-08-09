using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;

namespace UrbanWoolen.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StoreController(ApplicationDbContext context)
        {
            _context = context;
        }

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



        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }
    }

}
