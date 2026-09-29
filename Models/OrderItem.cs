using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; }



        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
    }
}