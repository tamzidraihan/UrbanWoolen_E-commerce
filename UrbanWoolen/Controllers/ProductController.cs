using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrbanWoolen.Data;
using UrbanWoolen.Models;
using Microsoft.AspNetCore.Http;
using UrbanWoolen.Models.ViewModels;
using System.Text;

namespace UrbanWoolen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            SizeChart? chart = null;
            if (product.Category != ProductCategory.Accessories)
            {
                chart = await _context.SizeCharts
                    .Include(sc => sc.Items)
                    .Where(sc => sc.Category == product.Category)
                    .OrderBy(sc => sc.Id)
                    .FirstOrDefaultAsync();
            }

            var vm = new UrbanWoolen.Models.ViewModels.ProductDetailsViewModel
            {
                Product = product,
                SizeChart = chart,
                SelectedRegion = chart?.Region ?? "BD",
                SelectedUnit = chart?.Unit ?? "cm",
                AvailableRegions = await _context.SizeCharts
                    .Where(c => c.Category == product.Category)
                    .Select(c => c.Region)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync()
            };

            return View(vm);
        }


        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(imageFile.FileName);
                    var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)); // Ensure path exists

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    product.ImageUrl = "/images/" + fileName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, [FromForm] IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            var productInDb = await _context.Products.FindAsync(product.Id);
            if (productInDb == null)
            {
                return NotFound();
            }

            productInDb.Name = product.Name;
            productInDb.Description = product.Description;
            productInDb.Price = product.Price;
            productInDb.Category = product.Category;
            productInDb.Stock = product.Stock;
            productInDb.IsWinterCollection = product.IsWinterCollection;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                productInDb.ImageUrl = "/images/" + fileName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Inventory(
    string? search, ProductCategory? category, string stockStatus = "all",
    string sortBy = "name", int page = 1, int pageSize = 20)
        {
            var q = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(p => p.Name.ToLower().Contains(s)
                              || (p.Sku != null && p.Sku.ToLower().Contains(s)));
            }
            if (category.HasValue) q = q.Where(p => p.Category == category.Value);

            // stock status filter
            q = stockStatus switch
            {
                "out" => q.Where(p => p.Stock <= 0),
                "low" => q.Where(p => p.Stock > 0 && p.Stock <= p.ReorderPoint),
                "in" => q.Where(p => p.Stock > p.ReorderPoint),
                _ => q
            };

            // project to rows (compute available, value)
            var rowsQ = q.Select(p => new InventoryRow
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                Category = p.Category,
                Sku = p.Sku,
                Price = p.Price,
                CostPrice = p.CostPrice,
                Stock = p.Stock,
                Reserved = p.Reserved,
                ReorderPoint = p.ReorderPoint
            });

            // sort
            rowsQ = sortBy switch
            {
                "stock" => rowsQ.OrderByDescending(r => r.Stock),
                "value" => rowsQ.OrderByDescending(r => r.StockValue),
                _ => rowsQ.OrderBy(r => r.Name)
            };

            var total = await rowsQ.CountAsync();
            var items = await rowsQ.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new InventoryIndexViewModel
            {
                Filter = new InventoryFilter
                {
                    Search = search,
                    Category = category,
                    StockStatus = stockStatus,
                    SortBy = sortBy,
                    Page = page,
                    PageSize = pageSize
                },
                Items = items,
                TotalCount = total
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adjust(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.ProductName = p.Name;
            return View(new InventoryTransaction { ProductId = id, Change = 0, Reason = InventoryReason.ManualAdjust });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adjust(InventoryTransaction tx)
        {
            if (tx.Change == 0)
                ModelState.AddModelError(nameof(tx.Change), "Change cannot be zero.");

            var p = await _context.Products.FindAsync(tx.ProductId);
            if (p == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ProductName = p.Name;
                return View(tx);
            }

            // apply change
            p.Stock += tx.Change;
            if (tx.Change > 0) p.LastRestockedAt = DateTime.UtcNow;

            tx.CreatedAt = DateTime.UtcNow;
            tx.PerformedByUserId = User?.Identity?.Name;

            _context.InventoryTransactions.Add(tx);
            await _context.SaveChangesAsync();

            TempData["InvMsg"] = $"Stock adjusted for {p.Name}: {(tx.Change > 0 ? "+" : "")}{tx.Change}";
            return RedirectToAction(nameof(Inventory));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Movements(int productId, int take = 50)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            var tx = await _context.InventoryTransactions
                .Where(t => t.ProductId == productId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(take)
                .ToListAsync();

            ViewBag.Product = product;
            return View(tx); // you'll add a simple table view
        }

        [Authorize(Roles = "Admin")]
        public async Task<FileResult> ExportInventoryCsv()
        {
            var items = await _context.Products.AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,SKU,Name,Category,Price,CostPrice,Stock,Reserved,ReorderPoint,StockValue");
            foreach (var p in items)
            {
                var value = p.CostPrice * p.Stock;
                sb.AppendLine($"{p.Id},\"{p.Sku}\",\"{p.Name}\",{p.Category},{p.Price:0.00},{p.CostPrice:0.00},{p.Stock},{p.Reserved},{p.ReorderPoint},{value:0.00}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"inventory-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var since30 = DateTime.UtcNow.AddDays(-30);
            var since90 = DateTime.UtcNow.AddDays(-90);

            // Base queries
            var ordersQ = _context.Orders
                .Include(o => o.Items)
                .AsNoTracking();

            // All-time KPIs
            var totalOrders = await ordersQ.CountAsync();
            var totalSales = await _context.OrderItems
                .SumAsync(i => (decimal?)(i.Price * i.Quantity)) ?? 0m;
            var totalProducts = await _context.Products.CountAsync();
            var lowStockCount = await _context.Products.CountAsync(p => p.Stock <= 5);
            var unitsSoldAll = await _context.OrderItems.SumAsync(i => (int?)i.Quantity) ?? 0;
            var aov = totalOrders > 0 ? Math.Round(totalSales / totalOrders, 2) : 0m;

            // Last 30 days
            var orders30Q = ordersQ.Where(o => o.OrderDate >= since30);
            var sales30 = await orders30Q
                .SelectMany(o => o.Items.Select(i => i.Price * i.Quantity))
                .SumAsync();
            var orders30 = await orders30Q.CountAsync();

            // Distinct active customers in 30d (by UserId)
            var activeCustomers30 = await orders30Q
                .Select(o => o.UserId)
                .Where(uid => uid != null)
                .Distinct()
                .CountAsync();

            // Repeat purchase rate (last 90d)
            var buyersCounts90 = await ordersQ
                .Where(o => o.OrderDate >= since90 && o.UserId != null)
                .GroupBy(o => o.UserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            var buyers90 = buyersCounts90.Count;
            var repeaters90 = buyersCounts90.Count(x => x.Count >= 2);
            var repeatRate90 = buyers90 > 0 ? Math.Round((decimal)repeaters90 / buyers90, 3) : 0m;

            // Trends (last 30 days)
            var days = Enumerable.Range(0, 30)
                .Select(offset => DateTime.UtcNow.Date.AddDays(-29 + offset))
                .ToList();

            var revenueByDayRaw = await orders30Q
                .SelectMany(o => o.Items.Select(i => new { Day = o.OrderDate.Date, Amount = i.Price * i.Quantity }))
                .GroupBy(x => x.Day)
                .Select(g => new { Day = g.Key, Revenue = g.Sum(x => x.Amount) })
                .ToListAsync();

            var ordersByDayRaw = await orders30Q
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var labels = days.Select(d => d.ToString("MMM dd")).ToList();
            var revenueSeries = days.Select(d => revenueByDayRaw.FirstOrDefault(x => x.Day == d)?.Revenue ?? 0m).ToList();
            var ordersSeries = days.Select(d => ordersByDayRaw.FirstOrDefault(x => x.Day == d)?.Count ?? 0).ToList();

            // Top products (by revenue, last 30d) — from OrderItems only (uses ProductId/ProductName in OrderItem)
            var topProducts = await orders30Q
                .SelectMany(o => o.Items.Select(i => new { i.ProductId, i.ProductName, Rev = i.Price * i.Quantity, Qty = i.Quantity }))
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new TopProductRow
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.ProductName,
                    Quantity = g.Sum(x => x.Qty),
                    Revenue = g.Sum(x => x.Rev)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            // Category mix (join OrderItems to Products to read Category)
            var categoryMix = await orders30Q
                .SelectMany(o => o.Items.Select(i => new { i.ProductId, Amount = i.Price * i.Quantity }))
                .Join(_context.Products,
                      oi => oi.ProductId,
                      p => p.Id,
                      (oi, p) => new { p.Category, oi.Amount })
                .GroupBy(x => x.Category)
                .Select(g => new CategorySlice
                {
                    Category = g.Key.ToString(),
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            // Low stock list (≤5)
            var lowStock = await _context.Products
                .Where(p => p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(10)
                .Select(p => new LowStockRow
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Stock = p.Stock
                })
                .ToListAsync();

            // Recent orders (last 10)
            var recentOrders = await ordersQ
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderRow
                {
                    OrderId = o.Id,
                    CreatedAt = o.OrderDate,
                    CustomerEmail = o.UserId,              // if you store email elsewhere, adjust here
                    Status = o.Status.ToString(),          // OrderStatus enum -> string
                    Total = o.Items.Sum(i => i.Price * i.Quantity)
                })
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalOrders = totalOrders,
                TotalSales = Math.Round(totalSales, 2),
                TotalProducts = totalProducts,
                LowStockCount = lowStockCount,

                Sales30d = Math.Round(sales30, 2),
                Orders30d = orders30,
                AovAllTime = aov,
                UnitsSoldAllTime = unitsSoldAll,
                ActiveCustomers30d = activeCustomers30,
                RepeatPurchaseRate90d = repeatRate90,

                DaysLabels = labels,
                RevenueByDay = revenueSeries,
                OrdersByDay = ordersSeries,

                TopProducts30d = topProducts,
                CategoryMix30d = categoryMix,
                LowStockItems = lowStock,
                RecentOrders = recentOrders
            };

            return View(vm);
        }


    }
}
