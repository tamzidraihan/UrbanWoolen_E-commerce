using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UrbanWoolen.Models.ViewModels
{
    public class CheckoutViewModel
    {
        
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } 
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;   
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        // Cart Summary (display only)
        [BindNever]
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        [BindNever]
        public decimal TotalAmount => CartItems?.Sum(i => i.Price * i.Quantity) ?? 0;
    }
}
