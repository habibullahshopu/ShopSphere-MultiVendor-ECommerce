namespace ShopSphere.Services
{
    public interface IAISearchService
    {
        Task<AIProductSearchQuery> UnderstandSearchAsync(string searchText);
    }

    public class AIProductSearchQuery
    {
        public string? Keyword { get; set; }

        public string? Category { get; set; }

        public decimal? MaxPrice { get; set; }

        public decimal? MinPrice { get; set; }
    }
}