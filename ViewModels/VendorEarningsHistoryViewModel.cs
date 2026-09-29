using System;
using System.Collections.Generic;

namespace ShopSphere.ViewModels
{
    public class VendorEarningsHistoryViewModel
    {
        // Summary
        public string StoreName { get; set; } = string.Empty;

        public decimal CommissionRate { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalSales { get; set; }

        public decimal TotalCommission { get; set; }

        public decimal NetEarnings { get; set; }

        // Earnings History
        public List<VendorEarningItemViewModel> Earnings
        { get; set; }
            = new List<VendorEarningItemViewModel>();
    }

    public class VendorEarningItemViewModel
    {
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        public decimal CommissionAmount { get; set; }

        public decimal NetEarnings { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
    }
}