using System.Collections.Generic;

namespace UrbanWoolen.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = default!;
        public SizeChart? SizeChart { get; set; }

        // Region/Unit controls (for public page)
        public string? SelectedRegion { get; set; }     // e.g., "BD"
        public string SelectedUnit { get; set; } = "cm"; // "cm" or "in"

        // Regions with charts available for this category
        public List<string> AvailableRegions { get; set; } = new();
    }
}
