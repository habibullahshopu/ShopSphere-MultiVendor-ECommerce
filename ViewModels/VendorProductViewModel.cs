using System.ComponentModel.DataAnnotations;

namespace ShopSphere.ViewModels
{
    public class VendorProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
    }
}