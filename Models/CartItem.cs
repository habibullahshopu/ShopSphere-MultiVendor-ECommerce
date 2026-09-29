using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSphere.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        public Cart? Cart { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        [NotMapped]
        public decimal Subtotal =>
            (Product?.Price ?? 0) * Quantity;
    }
}