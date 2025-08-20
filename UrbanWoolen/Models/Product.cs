using System.ComponentModel.DataAnnotations;

namespace UrbanWoolen.Models
{
    public enum ProductCategory
    {
        [Display(Name = "Men")]
        Men,
        [Display(Name = "Women")]
        Women,
        [Display(Name = "Kids")]
        Kids,
        [Display(Name = "Accessories")]
        Accessories
    }


    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Category")]
        public ProductCategory Category { get; set; }

        [Display(Name = "Available Stock")]
        [Range(0, 10000)]
        public int Stock { get; set; }
        public List<Review> Reviews { get; set; } = new();
        public bool IsFeatured { get; set; } = false;
        public bool IsWinterCollection { get; set; } = false;

        // Inventory metadata (NEW)
        [MaxLength(64)]
        public string? Sku { get; set; }

        [Range(0, 1000000)]
        public decimal CostPrice { get; set; } = 0m;   // per unit cost (BDT)

        [Range(0, 100000)]
        public int ReorderPoint { get; set; } = 5;     // threshold for "Low"

        public int Reserved { get; set; } = 0;         // units committed to orders/carts

        public DateTime? LastRestockedAt { get; set; } // for aging/velocity later


    }
}
