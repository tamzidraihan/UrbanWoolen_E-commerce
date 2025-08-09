using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UrbanWoolen.Models.ViewModels
{
    public class CheckoutViewModel
    {
        
        public string FullName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        // Cart Summary (display only)
        [BindNever]
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        [BindNever]
        public decimal TotalAmount => CartItems?.Sum(i => i.Price * i.Quantity) ?? 0;
    }
}
