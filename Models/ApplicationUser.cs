using Microsoft.AspNetCore.Identity;

namespace ShopSphere.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Vendor Information
        public bool IsVendor { get; set; } = false;

        public bool IsVendorApproved { get; set; } = false;

        public DateTime? VendorApprovedAt { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }
    }
}