using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UrbanWoolen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SizeChartItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SizeChartItemsController(ApplicationDbContext context) { _context = context; }

        // GET: SizeChartItems/Create?chartId=1
        public IActionResult Create(int chartId)
        {
            ViewBag.ChartId = chartId;
            return View(new SizeChartItem { SizeChartId = chartId });
        }

        // --- Helpers ---
        private static decimal? ParseNullableDecimal(string key, IFormCollection form)
        {
            if (!form.ContainsKey(key)) return null;
            var raw = form[key].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Normalize comma → dot to be culture-safe
            raw = raw.Replace(',', '.');

            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                ? d
                : (decimal?)null;
        }

        // Remove existing ModelState entry so stale binder errors don't stick around
        private void RemoveModelStateKey(string key)
        {
            if (ModelState.ContainsKey(key))
                ModelState.Remove(key);
        }

        private void NormalizeNumericFields(SizeChartItem item)
        {
            item.Chest = ParseNullableDecimal(nameof(SizeChartItem.Chest), Request.Form);
            item.Waist = ParseNullableDecimal(nameof(SizeChartItem.Waist), Request.Form);
            item.Length = ParseNullableDecimal(nameof(SizeChartItem.Length), Request.Form);
            item.Hip = ParseNullableDecimal(nameof(SizeChartItem.Hip), Request.Form);
            item.Inseam = ParseNullableDecimal(nameof(SizeChartItem.Inseam), Request.Form);
            item.FootLength = ParseNullableDecimal(nameof(SizeChartItem.FootLength), Request.Form);

            // Drop stale binder errors for ALL numeric fields (re-validate from normalized values)
            RemoveModelStateKey(nameof(SizeChartItem.Chest));
            RemoveModelStateKey(nameof(SizeChartItem.Waist));
            RemoveModelStateKey(nameof(SizeChartItem.Length));
            RemoveModelStateKey(nameof(SizeChartItem.Hip));
            RemoveModelStateKey(nameof(SizeChartItem.Inseam));
            RemoveModelStateKey(nameof(SizeChartItem.FootLength));
        }

        private void RelaxIrrelevantFields(SizeChart chart)
        {
            // For fields that don't apply to this chart type, ensure no validation complaints
            void Remove(string key) => RemoveModelStateKey(key);

            switch (chart.ChartType)
            {
                case ChartType.Pants:
                    // Relevant: Waist, Hip, Inseam, Length
                    Remove(nameof(SizeChartItem.Chest));
                    Remove(nameof(SizeChartItem.FootLength));
                    break;

                case ChartType.Shoes:
                    // Relevant: FootLength only
                    Remove(nameof(SizeChartItem.Chest));
                    Remove(nameof(SizeChartItem.Waist));
                    Remove(nameof(SizeChartItem.Length));
                    Remove(nameof(SizeChartItem.Hip));
                    Remove(nameof(SizeChartItem.Inseam));
                    break;

                default:
                    // Tops/General: Chest, Waist, Length
                    Remove(nameof(SizeChartItem.Hip));
                    Remove(nameof(SizeChartItem.Inseam));
                    Remove(nameof(SizeChartItem.FootLength));
                    break;
            }
        }

        // POST: SizeChartItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SizeChartItem item)
        {
            var chart = await _context.SizeCharts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == item.SizeChartId);

            if (chart == null)
            {
                ModelState.AddModelError("", "Parent size chart not found.");
                ViewBag.ChartId = item.SizeChartId;
                return View(item);
            }

            // 1) Normalize numbers (comma → dot), remove stale binder errors
            NormalizeNumericFields(item);

            // 2) Relax fields that don't apply to this chart type
            RelaxIrrelevantFields(chart);

            // 3) Re-validate the cleaned model (ensure Size is provided, etc.)
            ModelState.Clear();
            // Avoid “SizeChart field is required” on POST (we only post SizeChartId)
            if (ModelState.ContainsKey(nameof(SizeChartItem.SizeChart)))
                ModelState.Remove(nameof(SizeChartItem.SizeChart));
            TryValidateModel(item);

            if (!ModelState.IsValid)
            {
                ViewBag.ChartId = item.SizeChartId;
                return View(item);
            }

            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "SizeCharts", new { id = item.SizeChartId });
        }


        // GET: SizeChartItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.SizeChartItems.Include(i => i.SizeChart).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: SizeChartItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SizeChartItem item)
        {
            if (id != item.Id) return NotFound();

            var chart = await _context.SizeCharts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == item.SizeChartId);

            if (chart == null)
            {
                ModelState.AddModelError("", "Parent size chart not found.");
                return View(item);
            }

            // 1) Normalize numbers, purge stale binder errors
            NormalizeNumericFields(item);

            // 2) Relax irrelevant fields by chart type
            RelaxIrrelevantFields(chart);

            // 3) Re-validate against normalized values
            ModelState.Clear();
            // Avoid “SizeChart field is required” on POST (we only post SizeChartId)
            if (ModelState.ContainsKey(nameof(SizeChartItem.SizeChart)))
                ModelState.Remove(nameof(SizeChartItem.SizeChart));

            TryValidateModel(item);

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "SizeCharts", new { id = item.SizeChartId });
        }


        // GET: SizeChartItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.SizeChartItems.Include(i => i.SizeChart).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: SizeChartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.SizeChartItems.FindAsync(id);
            if (item == null) return NotFound();
            var chartId = item.SizeChartId;
            _context.SizeChartItems.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "SizeCharts", new { id = chartId });
        }
    }
}
