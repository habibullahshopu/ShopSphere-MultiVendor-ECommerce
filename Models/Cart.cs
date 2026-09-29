using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
    }
}