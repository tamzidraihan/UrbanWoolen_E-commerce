using System;
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
    public class DiscountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DiscountController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var list = await _context.Discounts
                .Include(d => d.Product)
                .OrderByDescending(d => d.StartsAt)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Create()
        {
            ViewBag.Products = new SelectList(_context.Products.OrderBy(p => p.Name), "Id", "Name");
            return View(new Discount { StartsAt = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Discount model)
        {
            if (model.EndsAt != null && model.EndsAt < model.StartsAt)
                ModelState.AddModelError(nameof(model.EndsAt), "End date must be after start date.");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = new SelectList(_context.Products.OrderBy(p => p.Name), "Id", "Name", model.ProductId);
                return View(model);
            }

            _context.Discounts.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var d = await _context.Discounts.FindAsync(id);
            if (d == null) return NotFound();
            ViewBag.Products = new SelectList(_context.Products.OrderBy(p => p.Name), "Id", "Name", d.ProductId);
            return View(d);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Discount model)
        {
            if (id != model.Id) return NotFound();

            if (model.EndsAt != null && model.EndsAt < model.StartsAt)
                ModelState.AddModelError(nameof(model.EndsAt), "End date must be after start date.");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = new SelectList(_context.Products.OrderBy(p => p.Name), "Id", "Name", model.ProductId);
                return View(model);
            }

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var d = await _context.Discounts.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();
            return View(d);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var d = await _context.Discounts.FindAsync(id);
            if (d == null) return NotFound();
            _context.Discounts.Remove(d);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
