namespace ShopSphere.Services
{
    public interface IAIInventoryPredictionService
    {
        Task<List<InventoryPredictionResult>> PredictInventoryAsync(
            InventoryPredictionInput input);
    }

    public class InventoryPredictionInput
    {
        public List<InventoryProductData> Products { get; set; }
            = new();
    }

    public class InventoryProductData
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public string? CategoryName { get; set; }

        public int CurrentStock { get; set; }

        public int ItemsSold { get; set; }

        public int DaysAnalyzed { get; set; }
    }

    public class InventoryPredictionResult
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public int CurrentStock { get; set; }

        public decimal PredictedDailySales { get; set; }

        public int EstimatedDaysUntilStockout { get; set; }

        public string RiskLevel { get; set; }
            = string.Empty;

        public int RecommendedRestockQuantity { get; set; }

        public string Explanation { get; set; }
            = string.Empty;
    }
}