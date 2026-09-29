using System;

namespace ShopSphere.ViewModels
{
    public class AdminVendorViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string StoreName { get; set; } = string.Empty;

        public string? StoreDescription { get; set; }

        public string? StoreAddress { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public bool IsApproved { get; set; }

        public bool IsActive { get; set; }

        public decimal CommissionRate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // User Information
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}