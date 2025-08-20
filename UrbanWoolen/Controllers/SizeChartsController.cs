using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;
using UrbanWoolen.Models;

namespace UrbanWoolen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SizeChartsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SizeChartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SizeCharts
        public async Task<IActionResult> Index()
        {
            var charts = await _context.SizeCharts
                .Include(c => c.Items)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Region)
                .ToListAsync();

            return View(charts);
        }

        // GET: SizeCharts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var chart = await _context.SizeCharts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chart == null) return NotFound();

            return View(chart);
        }

        // GET: SizeCharts/Create
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(
                System.Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>()
                    .Select(v => new { Id = (int)v, Name = v.ToString() }),
                "Id", "Name"
            );
            ViewBag.ChartTypes = new SelectList(
                System.Enum.GetValues(typeof(ChartType)).Cast<ChartType>()
                    .Select(v => new { Id = (int)v, Name = v.ToString() }),
                "Id", "Name"
            );
            return View();
        }

        // POST: SizeCharts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SizeChart chart)
        {
            if (!ModelState.IsValid)
            {
                return View(chart);
            }
            _context.Add(chart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: SizeCharts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var chart = await _context.SizeCharts.FindAsync(id);
            if (chart == null) return NotFound();

            ViewBag.Categories = new SelectList(
                System.Enum.GetValues(typeof(ProductCategory)).Cast<ProductCategory>()
                    .Select(v => new { Id = (int)v, Name = v.ToString() }),
                "Id", "Name", (int)chart.Category
            );
            ViewBag.ChartTypes = new SelectList(
                System.Enum.GetValues(typeof(ChartType)).Cast<ChartType>()
                    .Select(v => new { Id = (int)v, Name = v.ToString() }),
                "Id", "Name", (int)chart.ChartType
            );
            return View(chart);
        }

        // POST: SizeCharts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SizeChart chart)
        {
            if (id != chart.Id) return NotFound();
            if (!ModelState.IsValid) return View(chart);

            _context.Update(chart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: SizeCharts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var chart = await _context.SizeCharts.FirstOrDefaultAsync(m => m.Id == id);
            if (chart == null) return NotFound();
            return View(chart);
        }

        // POST: SizeCharts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chart = await _context.SizeCharts.FindAsync(id);
            if (chart != null) _context.SizeCharts.Remove(chart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
