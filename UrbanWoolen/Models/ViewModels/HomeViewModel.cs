using System.Collections.Generic;

namespace UrbanWoolen.Models.ViewModels
{
    public class DiscountProductVM
    {
        public Product Product { get; set; } = default!;
        public Discount Discount { get; set; } = default!;
    }

    public class HomeViewModel
    {
        // Discounts section
        public List<DiscountProductVM> ActiveDiscounts { get; set; } = new();

        // NEW: New Arrivals section
        public List<Product> NewArrivals { get; set; } = new();
    }
}
