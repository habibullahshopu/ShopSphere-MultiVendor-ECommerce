using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        // Vendor User
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // Store Information
        [Required]
        [StringLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? StoreDescription { get; set; }

        [StringLength(250)]
        public string? StoreLogo { get; set; }

        [StringLength(500)]
        public string? StoreAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        // Vendor Status
        public bool IsApproved { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime? ApprovedAt { get; set; }

        // Commission
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionRate { get; set; } = 10.00m;

        // Dates
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}