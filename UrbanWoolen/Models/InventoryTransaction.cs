using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UrbanWoolen.Models
{
    public enum InventoryReason
    {
        Purchase = 0,         // incoming from supplier
        Sale = 1,             // order shipped (decrement)
        Return = 2,           // customer return (increment)
        ManualAdjust = 3,     // ad-hoc correction
        Cancel = 4,           // order cancel (increment back)
        Other = 9
    }

    public class InventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = default!;

        // Positive = add stock, Negative = reduce stock
        public int Change { get; set; }

        public InventoryReason Reason { get; set; } = InventoryReason.ManualAdjust;

        [MaxLength(128)]
        public string? Reference { get; set; }  // e.g., PO#123, Order#987

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: which admin did it (Identity UserId)
        public string? PerformedByUserId { get; set; }
    }
}
