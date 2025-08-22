using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using UrbanWoolen.Models.ViewModels;

namespace UrbanWoolen.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin View: All Orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            var model = orders.Select(o => new AdminOrderViewModel
            {
                Order = o,
                Email = users.FirstOrDefault(u => u.Id == o.UserId)?.Email ?? "Unknown"
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            // CHANGED: Load order WITH items so we can adjust stock
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status; // CHANGED: Remember previous status

            // Only deduct stock when transitioning into Delivered for the first time
            if (oldStatus != OrderStatus.Delivered && status == OrderStatus.Delivered)
            {
                // NEW: transaction for consistency
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Pull all affected products once
                    var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id);

                    foreach (var item in order.Items)
                    {
                        if (!products.TryGetValue(item.ProductId, out var product))
                            continue; // product removed? skip gracefully

                        // Decrease stock by ordered quantity
                        product.Stock -= item.Quantity;

                        // Optional guard: don't go below zero (remove if you want negatives)
                        if (product.Stock < 0) product.Stock = 0;

                        // OPTIONAL: record an inventory transaction (uses your existing model)
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            ProductId = item.ProductId,
                            Change = -item.Quantity,
                            // If you have a specific reason like InventoryReason.OrderDelivered, use that.
                            Reason = InventoryReason.ManualAdjust,
                            CreatedAt = DateTime.UtcNow,
                            PerformedByUserId = User?.Identity?.Name
                        });
                    }

                    // Finally set the new status
                    order.Status = status;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    TempData["CartMessage"] = $"Failed to update Order #{id} (inventory adjustment error).";
                    return RedirectToAction(nameof(AllOrders));
                }
            }
            else
            {
                // Any other status change: just set it (no stock change)
                order.Status = status;
                await _context.SaveChangesAsync();
            }

            TempData["CartMessage"] = $"Order #{id} status updated to {status}.";
            return RedirectToAction(nameof(AllOrders));
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}
