using System.ComponentModel.DataAnnotations;

namespace ShopSphere.ViewModels
{
    public class VendorRegistrationViewModel
    {
        [Required]
        [Display(Name = "Store Name")]
        [StringLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [Display(Name = "Store Description")]
        [StringLength(1000)]
        public string? StoreDescription { get; set; }

        [Display(Name = "Store Address")]
        [StringLength(500)]
        public string? StoreAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }
    }
}