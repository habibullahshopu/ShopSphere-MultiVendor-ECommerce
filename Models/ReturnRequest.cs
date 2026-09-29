using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; } = 0;

        [StringLength(30)]
        public string? RefundStatus { get; set; }

        [StringLength(1000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public Order? Order { get; set; }

        public ApplicationUser? User { get; set; }
    }
}