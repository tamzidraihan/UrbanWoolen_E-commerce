using System;
using System.ComponentModel.DataAnnotations;

namespace UrbanWoolen.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string Comment { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        public string? UserName { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }
    }

}
