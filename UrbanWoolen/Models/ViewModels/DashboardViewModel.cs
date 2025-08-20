using System;
using System.Collections.Generic;

namespace UrbanWoolen.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Existing cards you already show
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }

        // New KPIs
        public decimal Sales30d { get; set; }
        public int Orders30d { get; set; }
        public decimal AovAllTime { get; set; }
        public int UnitsSoldAllTime { get; set; }
        public int ActiveCustomers30d { get; set; }  // distinct buyers in last 30d
        public decimal RepeatPurchaseRate90d { get; set; } // 0..1 fraction

        // Trends (aligned arrays for Chart.js)
        public List<string> DaysLabels { get; set; } = new();
        public List<decimal> RevenueByDay { get; set; } = new();
        public List<int> OrdersByDay { get; set; } = new();

        // Top products (last 30d)
        public List<TopProductRow> TopProducts30d { get; set; } = new();

        // Category mix (last 30d)
        public List<CategorySlice> CategoryMix30d { get; set; } = new();

        // Low stock list
        public List<LowStockRow> LowStockItems { get; set; } = new();

        // Recent orders table
        public List<RecentOrderRow> RecentOrders { get; set; } = new();
    }

    public class TopProductRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySlice
    {
        public string Category { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    public class LowStockRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int Stock { get; set; }
    }

    public class RecentOrderRow
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerEmail { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
    }
}
