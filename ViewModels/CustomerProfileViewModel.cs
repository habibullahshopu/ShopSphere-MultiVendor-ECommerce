using System.ComponentModel.DataAnnotations;

namespace ShopSphere.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string? PostalCode { get; set; }
    }
}