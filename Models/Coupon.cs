using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string DiscountType { get; set; } = "Percentage";

        [Required]
        [Range(0.01, 999999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Range(0, 999999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrderAmount { get; set; } = 0;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(0, 999999999)]
        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}