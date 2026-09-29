using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class Payment
    {
        public int Id { get; set; }

        // Order relation
        [Required]
        public int OrderId { get; set; }

        public Order? Order { get; set; }


        // SSLCOMMERZ Transaction ID
        [StringLength(100)]
        public string? TranId { get; set; }


        // SSLCOMMERZ Validation ID
        [StringLength(100)]
        public string? ValId { get; set; }


        // Gateway Session Key
        [StringLength(100)]
        public string? SessionKey { get; set; }


        // Bank Transaction ID
        [StringLength(150)]
        public string? BankTranId { get; set; }


        // Card / Gateway type
        [StringLength(100)]
        public string? CardType { get; set; }


        // Payment amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }


        // Currency
        [StringLength(10)]
        public string Currency { get; set; } = "BDT";


        // Pending / Paid / Failed / Cancelled / Refunded
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";


        // Gateway response status
        [StringLength(50)]
        public string? GatewayStatus { get; set; }


        // Gateway response message / error
        [StringLength(1000)]
        public string? GatewayResponse { get; set; }


        // Payment creation/update date
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}