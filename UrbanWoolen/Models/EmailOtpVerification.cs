
using System;
using System.ComponentModel.DataAnnotations;

namespace UrbanWoolen.Models
{
    public class EmailOtpVerification
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }

        public DateTime ExpiryTime { get; set; }

        public bool IsVerified { get; set; }
    }
}
