using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(0, 999999)]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==============================
        // Category
        // ==============================

        [Required]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        // ==============================
        // Vendor
        // ==============================

        // Nullable because existing products
        // may not belong to a vendor yet.
        public int? VendorId { get; set; }

        public Vendor? Vendor { get; set; }
    }
}