using System;
using System.Collections.Generic;

namespace ShopSphere.ViewModels
{
    public class VendorOrderViewModel
    {
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public decimal VendorSubtotal { get; set; }

        public decimal CommissionAmount { get; set; }

        public decimal VendorEarnings { get; set; }

        public List<VendorOrderItemViewModel> Items { get; set; }
            = new List<VendorOrderItemViewModel>();
    }

    public class VendorOrderItemViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}