namespace ShopSphere.Services
{
    public interface IAISalesPredictionService
    {
        Task<SalesPredictionResult> PredictSalesAsync(
            SalesPredictionInput input);
    }

    public class SalesPredictionInput
    {
        public List<SalesHistoryItem> SalesHistory { get; set; }
            = new();
    }

    public class SalesHistoryItem
    {
        public DateTime Date { get; set; }

        public decimal SalesAmount { get; set; }

        public int OrderCount { get; set; }

        public int ItemsSold { get; set; }
    }

    public class SalesPredictionResult
    {
        public decimal PredictedSales { get; set; }

        public int PredictedOrders { get; set; }

        public int PredictedItemsSold { get; set; }

        public string Trend { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;
    }
}