using System.Collections.Generic;

namespace UrbanWoolen.Models.ViewModels
{
    public class InventoryFilter
    {
        public string? Search { get; set; }
        public ProductCategory? Category { get; set; }
        public string StockStatus { get; set; } = "all"; // all | in | low | out
        public string SortBy { get; set; } = "name";     // name | stock | value
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class InventoryRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public ProductCategory Category { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int Stock { get; set; }
        public int Reserved { get; set; }
        public int Available => Stock - Reserved;
        public int ReorderPoint { get; set; }
        public decimal StockValue => CostPrice * Stock;
        public bool IsLow => Stock <= ReorderPoint;
        public bool IsOut => Stock <= 0;
    }

    public class InventoryIndexViewModel
    {
        public InventoryFilter Filter { get; set; } = new();
        public List<InventoryRow> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
