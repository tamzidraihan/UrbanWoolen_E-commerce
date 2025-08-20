using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UrbanWoolen.Models
{
    // Helps us support different measurement sets by apparel type later
    public enum ChartType
    {
        General = 0,   // Chest/Waist/Length
        Tops = 1,      // Chest/Waist/Length
        Pants = 2,     // Waist/Hip/Inseam/Length
        Shoes = 3      // FootLength only (usually)
    }

    public class SizeChart
    {
        public int Id { get; set; }

        [Required]
        public ProductCategory Category { get; set; }

        [Required, MaxLength(32)]
        public string Title { get; set; } = "Default Size Guide";

        // e.g., "BD", "US", "UK", "EU"
        [MaxLength(8)]
        public string Region { get; set; } = "BD";

        // "cm" or "in" (we seed "cm"); the UI can toggle later
        [MaxLength(4)]
        public string Unit { get; set; } = "cm";

        public ChartType ChartType { get; set; } = ChartType.Tops;

        public List<SizeChartItem> Items { get; set; } = new();
    }

    public class SizeChartItem
    {
        public int Id { get; set; }
        public int SizeChartId { get; set; }
        [ValidateNever]
        public SizeChart? SizeChart { get; set; }

        // e.g., "S", "M", "L", "32", "42"
        [Required, MaxLength(16)]
        public string Size { get; set; } = default!;

        // Nullable to allow different chart types without schema churn
        public decimal? Chest { get; set; }
        public decimal? Waist { get; set; }
        public decimal? Length { get; set; }
        public decimal? Hip { get; set; }
        public decimal? Inseam { get; set; }
        public decimal? FootLength { get; set; }
    }
}
