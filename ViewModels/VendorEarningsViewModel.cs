namespace ShopSphere.ViewModels
{
    public class VendorEarningsViewModel
    {
        public string StoreName { get; set; } = string.Empty;

        public decimal CommissionRate { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalSales { get; set; }

        public decimal TotalCommission { get; set; }

        public decimal NetEarnings { get; set; }
    }
}