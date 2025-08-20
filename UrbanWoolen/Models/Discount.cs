using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UrbanWoolen.Models
{
    public enum DiscountType { Percentage = 0, Flat = 1 }

    public class Discount
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Don't validate/bind navigation on POST; it's null during Create/Edit
        [ValidateNever]
        public Product? Product { get; set; }

        [Required]
        public DiscountType Type { get; set; } = DiscountType.Percentage;

        // If Percentage: 0–100 (15 = 15%); If Flat: amount in ৳
        [Range(0, 100000)]
        public decimal Value { get; set; }

        public DateTime StartsAt { get; set; } = DateTime.UtcNow;

        // null = open-ended
        public DateTime? EndsAt { get; set; }

        [NotMapped]
        public bool IsActive =>
            DateTime.UtcNow >= StartsAt &&
            (EndsAt == null || DateTime.UtcNow <= EndsAt.Value);

        // NULL-SAFE computed price; safe when Product is not loaded on POST
        [NotMapped]
        public decimal DiscountedPrice
        {
            get
            {
                var price = Product?.Price ?? 0m; // guard when Product == null
                return Type == DiscountType.Percentage
                    ? Math.Round(price * (1 - (Value / 100m)), 2)
                    : Math.Max(0, Math.Round(price - Value, 2));
            }
        }
    }
}
