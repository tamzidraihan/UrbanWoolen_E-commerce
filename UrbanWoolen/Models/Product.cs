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

    }
}
